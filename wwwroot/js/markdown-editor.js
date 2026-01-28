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
            el.style.backgroundColor = 'var(--bg-card)';
            el.style.borderColor = 'var(--border-medium)';
            el.style.color = 'var(--text-primary)';

            // Update elements that Quill sets explicitly
            const picks = el.querySelectorAll('.ql-picker, .ql-stroke, .ql-fill');
            picks.forEach(p => {
                if (p.classList.contains('ql-stroke')) p.style.stroke = 'var(--text-primary)';
                else if (p.classList.contains('ql-fill')) p.style.fill = 'var(--text-primary)';
                else p.style.color = 'var(--text-primary)';
            });
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
