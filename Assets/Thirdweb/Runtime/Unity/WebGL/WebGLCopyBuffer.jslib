mergeInto(LibraryManager.library, {
  ThirdwebCopyBuffer: function (textPtr) {
    var text = UTF8ToString(textPtr);

    if (navigator.clipboard && navigator.clipboard.writeText) {
      navigator.clipboard
        .writeText(text)
        .then(function () {
          console.log("Copied to clipboard:", text);
        })
        .catch(function (err) {
          console.warn("Failed to copy text with navigator.clipboard:", err);
          fallbackCopyText(text);
        });
    } else {
      fallbackCopyText(text);
    }

    function fallbackCopyText(textToCopy) {
      var input = document.createElement("textarea");
      input.value = textToCopy;
      input.style.position = "absolute";
      input.style.left = "-9999px";
      document.body.appendChild(input);
      input.select();
      document.execCommand("copy");
      document.body.removeChild(input);
      console.log("Copied to clipboard using fallback:", textToCopy);
    }
  },
});
