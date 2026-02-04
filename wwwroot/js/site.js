// Confirmar exclusão ao clicar em links com a classe .js-delete
document.addEventListener("DOMContentLoaded", function () {
  document.querySelectorAll(".js-delete").forEach(function (el) {
    el.addEventListener("click", function (e) {
      e.preventDefault();
      var url = this.getAttribute("href") || this.dataset.url;
      var row = this.closest("tr");
      var name = row
        ? row.querySelector("td:nth-child(2)")?.innerText || ""
        : "";
      var message =
        "Confirmar exclusão" + (name ? ' de "' + name.trim() + '"?' : "?");
      if (confirm(message)) {
        window.location = url;
      }
    });
  });
});

// Simple toast helper (requires Bootstrap 5)
function showToast(message, type = "info") {
  var toastEl = document.createElement("div");
  toastEl.className = "toast align-items-center text-bg-light border-0";
  toastEl.setAttribute("role", "alert");
  toastEl.setAttribute("aria-live", "assertive");
  toastEl.setAttribute("aria-atomic", "true");
  toastEl.innerHTML =
    '<div class="d-flex"><div class="toast-body">' +
    message +
    '</div><button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button></div>';
  document.body.appendChild(toastEl);
  var toast = new bootstrap.Toast(toastEl, { delay: 4000 });
  toast.show();
  toastEl.addEventListener("hidden.bs.toast", function () {
    toastEl.remove();
  });
}

// Currency mask for inputs with class .js-currency (Brazilian format)
(function () {
  function formatFromDigits(value) {
    if (!value) return "";
    var v = value.replace(/\D/g, "");
    var n = parseInt(v, 10);
    if (isNaN(n)) return "";
    return (n / 100).toLocaleString("pt-BR", {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    });
  }

  function bindCurrency(el) {
    // format initial value if any
    if (el.value) el.value = formatFromDigits(el.value);

    el.addEventListener("input", function (e) {
      var pos = el.selectionStart;
      var oldLen = el.value.length;
      el.value = formatFromDigits(el.value);
      var newLen = el.value.length;
      // try keep caret near the end
      el.selectionStart = el.selectionEnd = pos + (newLen - oldLen);
    });
  }

  document.addEventListener("DOMContentLoaded", function () {
    document.querySelectorAll(".js-currency").forEach(bindCurrency);

    // on submit, normalize currency inputs to plain numeric with dot as decimal separator
    document.querySelectorAll("form").forEach(function (form) {
      form.addEventListener("submit", function () {
        form.querySelectorAll(".js-currency").forEach(function (inp) {
          if (!inp.value) return;
          var normalized = inp.value.replace(/\./g, "").replace(",", ".");
          inp.value = normalized;
        });
      });
    });
  });
})();
