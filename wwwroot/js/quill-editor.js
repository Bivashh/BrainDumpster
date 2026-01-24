// wwwroot/js/quill-editor.js
let quill;

window.initQuill = (editorId) => {
    quill = new Quill(`#${editorId}`, {
        theme: "snow",
        modules: {
            toolbar: [
                ["bold", "italic", "underline"],
                [{ list: "ordered" }, { list: "bullet" }],
                [{ header: [1, 2, false] }],
                ["clean"]
            ]
        }
    });
};

window.getQuillHtml = () => {
    if (!quill) return "";
    return quill.root.innerHTML;
};

window.clearQuill = () => {
    if (quill) {
        quill.root.innerHTML = "";
    }
};