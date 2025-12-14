document.addEventListener('DOMContentLoaded', function() {
    let darkMode = false;
    const toggleBtn = document.getElementById('toggleTheme');
    const themeIcon = document.getElementById('themeIcon');

    if (!toggleBtn || !themeIcon) return;

    toggleBtn.addEventListener('click', function() {
        darkMode = !darkMode;
        if (darkMode) {
            document.body.style.backgroundColor = '#353434ff';
            document.body.style.color = '#63e1efff';
            themeIcon.classList.remove('bi-moon-fill');
            themeIcon.classList.add('bi-sun-fill');
        } else {
            document.body.style.backgroundColor = '#f0f2f5';
            document.body.style.color = '#000';
            themeIcon.classList.remove('bi-sun-fill');
            themeIcon.classList.add('bi-moon-fill');
        }
    });
});
