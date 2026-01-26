/**
 * Mero-Dainiki Markdown Editor Helper
 * Using EasyMDE for a premium "WYSWYG-like" Experience.
 */
window.markdownEditor = {
    instances: {},

    /**
     * Initializes EasyMDE on a textarea.
     * @param {string} elementId 
     * @param {object} dotNetHelper 
     */
    init: function (elementId, initialValue, dotNetHelper) {
        const textarea = document.getElementById(elementId);
        if (!textarea) return;

        const easyMDE = new EasyMDE({
            element: textarea,
            initialValue: initialValue || "",
            placeholder: "Write your heart out...",
            spellChecker: false,
            autosave: { enabled: false },
            status: ["words", "lines"],
            toolbar: [
                "bold", "italic", "heading", "|",
                "quote", "unordered-list", "ordered-list", "|",
                "link", "image", "|",
                "preview", "side-by-side", "fullscreen"
            ],
            syncSideBySidePreviewScroll: true,
            minHeight: "400px"
        });

        // Listen for changes and update Blazor
        easyMDE.codemirror.on("change", () => {
            const val = easyMDE.value();
            dotNetHelper.invokeMethodAsync('UpdateContentFromJS', val);
        });

        this.instances[elementId] = easyMDE;

        // Fix for Flexbox layout in Blazor
        const container = easyMDE.element.nextSibling;
        if (container && container.classList.contains('EasyMDEContainer')) {
            container.style.flex = "1";
            container.style.display = "flex";
            container.style.flexDirection = "column";
            const editor = container.querySelector('.CodeMirror');
            if (editor) editor.style.flex = "1";
        }
    },

    getValue: function (elementId) {
        return this.instances[elementId]?.value() || "";
    },

    setValue: function (elementId, value) {
        this.instances[elementId]?.value(value);
    },

    destroy: function (elementId) {
        if (this.instances[elementId]) {
            this.instances[elementId].toTextArea();
            delete this.instances[elementId];
        }
    },

    // Legacy backup methods (if still needed)
    wrapSelection: function (elementId, prefix, suffix) {
        const instance = this.instances[elementId];
        if (instance) {
            instance.codemirror.focus();
            const selected = instance.codemirror.getSelection();
            instance.codemirror.replaceSelection(prefix + selected + suffix);
            return instance.value();
        }
        return "";
    }
};
