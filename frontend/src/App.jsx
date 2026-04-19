import { useEffect, useState } from 'react'

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? ''

const defaultAuthForm = {
  username: '',
  password: ''
}

const defaultTransformForm = {
  resizeWidth: '',
  resizeHeight: '',
  cropWidth: '',
  cropHeight: '',
  cropX: '',
  cropY: '',
  rotate: '',
  watermark: '',
  quality: '85',
  format: 'jpeg',
  grayscale: false,
  sepia: false,
  flip: false,
  mirror: false
}

function App() {
  const [mode, setMode] = useState('login')
  const [authForm, setAuthForm] = useState(defaultAuthForm)
  const [token, setToken] = useState(() => localStorage.getItem('image-service-token') ?? '')
  const [username, setUsername] = useState(() => localStorage.getItem('image-service-username') ?? '')
  const [images, setImages] = useState([])
  const [selectedImageId, setSelectedImageId] = useState(null)
  const [previewUrl, setPreviewUrl] = useState('')
  const [uploadFile, setUploadFile] = useState(null)
  const [transformForm, setTransformForm] = useState(defaultTransformForm)
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')
  const [isBusy, setIsBusy] = useState(false)

  useEffect(() => {
    if (!token) {
      setImages([])
      setSelectedImageId(null)
      cleanupPreview()
      return
    }

    loadImages()
  }, [token])

  useEffect(() => {
    if (!token || !selectedImageId) {
      cleanupPreview()
      return
    }

    let active = true
    loadPreview(selectedImageId).then((url) => {
      if (!active) {
        URL.revokeObjectURL(url)
        return
      }

      cleanupPreview()
      setPreviewUrl(url)
    }).catch((loadError) => {
      if (active) {
        setError(loadError.message)
      }
    })

    return () => {
      active = false
    }
  }, [selectedImageId, token])

  useEffect(() => () => cleanupPreview(), [])

  function cleanupPreview() {
    setPreviewUrl((current) => {
      if (current) {
        URL.revokeObjectURL(current)
      }

      return ''
    })
  }

  function updateAuthField(event) {
    const { name, value } = event.target
    setAuthForm((current) => ({ ...current, [name]: value }))
  }

  function updateTransformField(event) {
    const { name, value, type, checked } = event.target
    setTransformForm((current) => ({
      ...current,
      [name]: type === 'checkbox' ? checked : value
    }))
  }

  async function handleAuthSubmit(event) {
    event.preventDefault()
    setIsBusy(true)
    setError('')
    setMessage('')

    try {
      const endpoint = mode === 'register' ? '/register' : '/login'
      const response = await apiFetch(endpoint, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(authForm)
      }, false)

      setToken(response.token)
      setUsername(response.username)
      localStorage.setItem('image-service-token', response.token)
      localStorage.setItem('image-service-username', response.username)
      setAuthForm(defaultAuthForm)
      setMessage(mode === 'register' ? 'Account created.' : 'Logged in.')
    } catch (submitError) {
      setError(submitError.message)
    } finally {
      setIsBusy(false)
    }
  }

  async function loadImages() {
    setIsBusy(true)
    setError('')

    try {
      const result = await apiFetch('/images?page=1&limit=20', {
        headers: authHeaders()
      })

      setImages(result.items)
      if (result.items.length > 0) {
        setSelectedImageId((current) => current && result.items.some((image) => image.id === current)
          ? current
          : result.items[0].id)
      } else {
        setSelectedImageId(null)
      }
    } catch (loadError) {
      setError(loadError.message)
    } finally {
      setIsBusy(false)
    }
  }

  async function handleUpload(event) {
    event.preventDefault()
    if (!uploadFile) {
      setError('Choose an image file first.')
      return
    }

    setIsBusy(true)
    setError('')
    setMessage('')

    try {
      const formData = new FormData()
      formData.append('file', uploadFile)

      const uploaded = await apiFetch('/images', {
        method: 'POST',
        headers: authHeaders(false),
        body: formData
      })

      setUploadFile(null)
      event.target.reset()
      setMessage(`Uploaded ${uploaded.fileName}.`)
      await loadImages()
      setSelectedImageId(uploaded.id)
    } catch (uploadError) {
      setError(uploadError.message)
    } finally {
      setIsBusy(false)
    }
  }

  async function handleTransform(event) {
    event.preventDefault()
    if (!selectedImageId) {
      setError('Select an image before applying a transformation.')
      return
    }

    setIsBusy(true)
    setError('')
    setMessage('')

    try {
      const payload = buildTransformPayload(transformForm)
      const transformed = await apiFetch(`/images/${selectedImageId}/transform`, {
        method: 'POST',
        headers: {
          ...authHeaders(),
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({ transformations: payload })
      })

      setMessage(`Created transformed image ${transformed.fileName}.`)
      await loadImages()
      setSelectedImageId(transformed.id)
    } catch (transformError) {
      setError(transformError.message)
    } finally {
      setIsBusy(false)
    }
  }

  function handleLogout() {
    localStorage.removeItem('image-service-token')
    localStorage.removeItem('image-service-username')
    setToken('')
    setUsername('')
    setImages([])
    setSelectedImageId(null)
    cleanupPreview()
    setMessage('Logged out.')
    setError('')
  }

  async function loadPreview(imageId) {
    const response = await fetch(withBaseUrl(`/images/${imageId}`), {
      headers: authHeaders()
    })

    if (!response.ok) {
      throw new Error(await readError(response))
    }

    const blob = await response.blob()
    return URL.createObjectURL(blob)
  }

  function authHeaders(includeJson = true) {
    return {
      ...(includeJson ? { Accept: 'application/json' } : {}),
      Authorization: `Bearer ${token}`
    }
  }

  async function apiFetch(path, options = {}, includeAuth = true) {
    const headers = new Headers(options.headers ?? {})
    if (includeAuth && token && !headers.has('Authorization')) {
      headers.set('Authorization', `Bearer ${token}`)
    }

    const response = await fetch(withBaseUrl(path), {
      ...options,
      headers
    })

    if (!response.ok) {
      throw new Error(await readError(response))
    }

    const contentType = response.headers.get('content-type') ?? ''
    if (contentType.includes('application/json')) {
      return response.json()
    }

    return response
  }

  function withBaseUrl(path) {
    return `${apiBaseUrl}${path}`
  }

  const selectedImage = images.find((image) => image.id === selectedImageId) ?? null

  return (
    <div className="page-shell">
      <header className="hero">
        <div>
          <p className="eyebrow">React Client</p>
          <h1>Image Processing Service</h1>
          <p className="hero-copy">
            Upload images, apply transformations, and browse your saved results from one small control panel.
          </p>
        </div>
        {token ? (
          <div className="session-card">
            <span className="session-label">Signed in as</span>
            <strong>{username}</strong>
            <button className="secondary-button" onClick={handleLogout} type="button">
              Log out
            </button>
          </div>
        ) : (
          <div className="session-card">
            <span className="session-label">Try it locally</span>
            <strong>{mode === 'register' ? 'Create account' : 'Sign in'}</strong>
          </div>
        )}
      </header>

      <main className="dashboard">
        {!token ? (
          <section className="panel auth-panel">
            <div className="panel-header">
              <h2>{mode === 'register' ? 'Create account' : 'Sign in'}</h2>
              <button
                className="text-button"
                onClick={() => setMode((current) => current === 'register' ? 'login' : 'register')}
                type="button"
              >
                {mode === 'register' ? 'Have an account?' : 'Need an account?'}
              </button>
            </div>

            <form className="stack" onSubmit={handleAuthSubmit}>
              <label>
                Username
                <input name="username" onChange={updateAuthField} required value={authForm.username} />
              </label>
              <label>
                Password
                <input name="password" onChange={updateAuthField} required type="password" value={authForm.password} />
              </label>
              <button className="primary-button" disabled={isBusy} type="submit">
                {isBusy ? 'Working...' : mode === 'register' ? 'Register' : 'Login'}
              </button>
            </form>
          </section>
        ) : (
          <>
            <section className="panel control-panel">
              <div className="panel-header">
                <h2>Upload</h2>
                <button className="secondary-button" disabled={isBusy} onClick={loadImages} type="button">
                  Refresh
                </button>
              </div>

              <form className="stack" onSubmit={handleUpload}>
                <label>
                  Select image
                  <input accept="image/*" onChange={(event) => setUploadFile(event.target.files?.[0] ?? null)} type="file" />
                </label>
                <button className="primary-button" disabled={isBusy} type="submit">
                  Upload image
                </button>
              </form>

              <div className="panel-header transform-header">
                <h2>Transform</h2>
                <span>{selectedImage ? selectedImage.fileName : 'No image selected'}</span>
              </div>

              <form className="transform-grid" onSubmit={handleTransform}>
                <label>
                  Resize width
                  <input name="resizeWidth" onChange={updateTransformField} placeholder="800" value={transformForm.resizeWidth} />
                </label>
                <label>
                  Resize height
                  <input name="resizeHeight" onChange={updateTransformField} placeholder="600" value={transformForm.resizeHeight} />
                </label>
                <label>
                  Crop width
                  <input name="cropWidth" onChange={updateTransformField} placeholder="500" value={transformForm.cropWidth} />
                </label>
                <label>
                  Crop height
                  <input name="cropHeight" onChange={updateTransformField} placeholder="500" value={transformForm.cropHeight} />
                </label>
                <label>
                  Crop X
                  <input name="cropX" onChange={updateTransformField} placeholder="0" value={transformForm.cropX} />
                </label>
                <label>
                  Crop Y
                  <input name="cropY" onChange={updateTransformField} placeholder="0" value={transformForm.cropY} />
                </label>
                <label>
                  Rotate
                  <input name="rotate" onChange={updateTransformField} placeholder="90" value={transformForm.rotate} />
                </label>
                <label>
                  Quality
                  <input name="quality" onChange={updateTransformField} placeholder="85" value={transformForm.quality} />
                </label>
                <label className="full-width">
                  Watermark text
                  <input name="watermark" onChange={updateTransformField} placeholder="sample" value={transformForm.watermark} />
                </label>
                <label className="full-width">
                  Output format
                  <select name="format" onChange={updateTransformField} value={transformForm.format}>
                    <option value="jpeg">JPEG</option>
                    <option value="png">PNG</option>
                    <option value="webp">WebP</option>
                  </select>
                </label>
                <label className="checkbox">
                  <input checked={transformForm.grayscale} name="grayscale" onChange={updateTransformField} type="checkbox" />
                  Grayscale
                </label>
                <label className="checkbox">
                  <input checked={transformForm.sepia} name="sepia" onChange={updateTransformField} type="checkbox" />
                  Sepia
                </label>
                <label className="checkbox">
                  <input checked={transformForm.flip} name="flip" onChange={updateTransformField} type="checkbox" />
                  Flip vertically
                </label>
                <label className="checkbox">
                  <input checked={transformForm.mirror} name="mirror" onChange={updateTransformField} type="checkbox" />
                  Mirror horizontally
                </label>
                <button className="primary-button full-width" disabled={isBusy || !selectedImage} type="submit">
                  Apply transformation
                </button>
              </form>
            </section>

            <section className="panel preview-panel">
              <div className="panel-header">
                <h2>Preview</h2>
                {selectedImage && (
                  <span>{selectedImage.width} x {selectedImage.height} • {selectedImage.format.toUpperCase()}</span>
                )}
              </div>

              <div className="preview-frame">
                {previewUrl ? (
                  <img alt={selectedImage?.fileName ?? 'Selected image'} src={previewUrl} />
                ) : (
                  <p>No image preview yet.</p>
                )}
              </div>

              {selectedImage && (
                <div className="meta-strip">
                  <span>{selectedImage.fileName}</span>
                  <span>{Math.round(selectedImage.sizeBytes / 1024)} KB</span>
                  <span>{selectedImage.isTransformed ? 'Transformed' : 'Original'}</span>
                </div>
              )}
            </section>

            <section className="panel gallery-panel">
              <div className="panel-header">
                <h2>Your images</h2>
                <span>{images.length} loaded</span>
              </div>

              <div className="gallery-list">
                {images.map((image) => (
                  <button
                    className={`gallery-item ${image.id === selectedImageId ? 'active' : ''}`}
                    key={image.id}
                    onClick={() => setSelectedImageId(image.id)}
                    type="button"
                  >
                    <strong>{image.fileName}</strong>
                    <span>{image.width} x {image.height}</span>
                    <span>{image.transformationSummary || 'Original upload'}</span>
                  </button>
                ))}
                {images.length === 0 && (
                  <div className="empty-state">Upload an image to get started.</div>
                )}
              </div>
            </section>
          </>
        )}

        {(message || error) && (
          <section className={`status-banner ${error ? 'error' : 'success'}`}>
            {error || message}
          </section>
        )}
      </main>
    </div>
  )
}

function buildTransformPayload(form) {
  const transformations = {
    flip: form.flip,
    mirror: form.mirror,
    quality: parseOptionalNumber(form.quality),
    format: form.format,
    filters: {
      grayscale: form.grayscale,
      sepia: form.sepia
    }
  }

  if (form.resizeWidth || form.resizeHeight) {
    transformations.resize = {
      width: parseOptionalNumber(form.resizeWidth),
      height: parseOptionalNumber(form.resizeHeight)
    }
  }

  if (form.cropWidth && form.cropHeight) {
    transformations.crop = {
      width: parseRequiredNumber(form.cropWidth),
      height: parseRequiredNumber(form.cropHeight),
      x: parseRequiredNumber(form.cropX || '0'),
      y: parseRequiredNumber(form.cropY || '0')
    }
  }

  if (form.rotate) {
    transformations.rotate = parseRequiredNumber(form.rotate)
  }

  if (form.watermark.trim()) {
    transformations.watermark = {
      text: form.watermark.trim()
    }
  }

  return transformations
}

function parseOptionalNumber(value) {
  if (value === '' || value == null) {
    return null
  }

  const parsed = Number(value)
  return Number.isFinite(parsed) ? parsed : null
}

function parseRequiredNumber(value) {
  const parsed = Number(value)
  if (!Number.isFinite(parsed)) {
    throw new Error('Transformation values must be valid numbers.')
  }

  return parsed
}

async function readError(response) {
  const contentType = response.headers.get('content-type') ?? ''

  if (contentType.includes('application/json')) {
    const payload = await response.json()
    return payload.detail || payload.message || payload.title || 'Request failed.'
  }

  return response.statusText || 'Request failed.'
}

export default App
