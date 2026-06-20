const apiUrl = (import.meta.env.VITE_API_URL || '').replace(/\/$/, '');

async function load() {
  const app = document.querySelector('#app');
  try {
    const res = await fetch(`${apiUrl}/data`);
    if (!res.ok) throw new Error(`API responded ${res.status}`);
    render(app, await res.json());
  } catch (err) {
    app.innerHTML = `<main style="font-family: system-ui; max-width: 42rem; margin: 4rem auto; padding: 0 1rem;">
      <h1>Couldn't load data</h1>
      <p style="color:#b00">${err.message}</p>
      <p>API URL: <code>${apiUrl || '(not set)'}</code></p>
    </main>`;
  }
}

function render(app, data) {
  const features = (data.features ?? [])
    .map((f) => `<li><strong>${f.name}</strong> — ${f.description}</li>`)
    .join('');

  app.innerHTML = `
    <main style="font-family: system-ui; max-width: 42rem; margin: 4rem auto; padding: 0 1rem; line-height: 1.5;">
      <h1>${data.title}</h1>
      <p style="color:#555;">${data.tagline}</p>
      <ul>${features}</ul>
      <p style="color:#888; font-size: 0.85rem;">Data served from R2 via the API.</p>
    </main>`;
}

load();
