/**
 * Mero-Dainiki Editor Helper
 * Switched to Quill.js for true WYSIWYG experience while maintaining Markdown storage.
 */
window.markdownEditor = {
    instances: {},
    turndownService: null,

    /**
     * Initializes Quill on a container.
     * @param {string} elementId 
     * @param {string} mdContent - The Markdown content from DB
     * @param {object} dotNetHelper 
     */
    init: function (elementId, mdContent, dotNetHelper) {
        const container = document.getElementById(elementId);
        if (!container) return;

        // Lazy init turndown
        if (!this.turndownService) {
            this.turndownService = new TurndownService({
                headingStyle: 'atx',
                codeBlockStyle: 'fenced'
            });
        }

        // Handle Markdown to HTML for Quill
        const initialHtml = mdContent ? marked.parse(mdContent) : "";

        // Create an editor container
        container.innerHTML = "";
        const editorDiv = document.createElement('div');
        editorDiv.id = elementId + "-quill";
        editorDiv.style.height = "100%";
        editorDiv.innerHTML = initialHtml;
        container.appendChild(editorDiv);

        const quill = new Quill('#' + editorDiv.id, {
            theme: 'snow',
            placeholder: "Write your heart out...",
            modules: {
                toolbar: [
                    [{ 'header': [1, 2, 3, false] }],
                    ['bold', 'italic', 'underline', 'strike'],
                    ['blockquote', 'code-block'],
                    [{ 'list': 'ordered' }, { 'list': 'bullet' }],
                    ['link', 'clean']
                ]
            }
        });

        // Debounced sync back to Blazor
        let timeout;
        quill.on('text-change', () => {
            clearTimeout(timeout);
            timeout = setTimeout(() => {
                const html = quill.root.innerHTML;
                const md = this.turndownService.turndown(html);
                dotNetHelper.invokeMethodAsync('UpdateContentFromJS', md);
            }, 300);
        });

        this.instances[elementId] = quill;

        // Final polish for themes
        this.applyThemeToQuill();
    },

    applyThemeToQuill: function () {
        const isDark = document.documentElement.classList.contains('dark');
        const containers = document.querySelectorAll('.ql-container, .ql-toolbar');
        containers.forEach(el => {
            if (isDark) {
                el.style.backgroundColor = '#1e1e24';
                el.style.borderColor = 'rgba(255,255,255,0.2)';
                el.style.color = '#f9fafb';

                // Active button styling
                const picks = el.querySelectorAll('.ql-picker, .ql-stroke, .ql-fill');
                picks.forEach(p => p.style.color = '#f9fafb');
            } else {
                el.style.backgroundColor = '#fff';
                el.style.borderColor = '#ccc';
                el.style.color = '#101828';
            }
        });
    },

    getValue: function (elementId) {
        const quill = this.instances[elementId];
        if (!quill) return "";
        return this.turndownService.turndown(quill.root.innerHTML);
    },

    setValue: function (elementId, mdValue) {
        const quill = this.instances[elementId];
        if (quill) {
            quill.root.innerHTML = marked.parse(mdValue || "");
        }
    },

    destroy: function (elementId) {
        if (this.instances[elementId]) {
            delete this.instances[elementId];
            const container = document.getElementById(elementId);
            if (container) container.innerHTML = "";
        }
    }
};

// Global observer for UI updates
const quillObserver = new MutationObserver(() => window.markdownEditor.applyThemeToQuill());
quillObserver.observe(document.documentElement, { attributes: true, attributeFilter: ['class'] });
