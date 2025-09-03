<script setup lang="ts">
import { ref, reactive } from 'vue';

// Type definition matching the C# VectorizationResult record
interface VectorizationResult {
  documentId: string; // Guid becomes string in JSON
  title: string;
  chunkCount: number;
}

// State for file upload
const fileUploadForm = reactive({
  file: null as File | null,
  isUploading: false,
  uploadResult: null as VectorizationResult | null,
  uploadError: null as string | null
});

// State for file path ingestion
const pathIngestForm = reactive({
  path: '',
  isIngesting: false,
  ingestResult: null as VectorizationResult | null,
  ingestError: null as string | null
});

// File input reference
const fileInput = ref<HTMLInputElement>();

// API base URL - adjust this to match your API
const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL as string) || 'http://localhost:5000'

function handleFileSelect(event: Event) {
  const target = event.target as HTMLInputElement;
  const file = target.files?.[0] || null;
  fileUploadForm.file = file;

  // Clear previous results
  fileUploadForm.uploadResult = null;
  fileUploadForm.uploadError = null;
}

async function uploadFile() {
  if (!fileUploadForm.file) {
    fileUploadForm.uploadError = 'Please select a file to upload.';
    return;
  }

  fileUploadForm.isUploading = true;
  fileUploadForm.uploadError = null;
  fileUploadForm.uploadResult = null;

  try {
    const formData = new FormData();
    formData.append('file', fileUploadForm.file);

    const response = await fetch(`${API_BASE_URL}/api/vectorization/ingest`, {
      method: 'POST',
      body: formData
    });

    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`Upload failed: ${response.status} ${errorText}`);
    }

    fileUploadForm.uploadResult = await response.json() as VectorizationResult;

    // Clear the file input
    if (fileInput.value) {
      fileInput.value.value = '';
    }
    fileUploadForm.file = null;

  } catch (error) {
    fileUploadForm.uploadError = error instanceof Error ? error.message : 'Upload failed';
  } finally {
    fileUploadForm.isUploading = false;
  }
}

async function ingestFilePath() {
  if (!pathIngestForm.path.trim()) {
    pathIngestForm.ingestError = 'Please enter a file path.';
    return;
  }

  pathIngestForm.isIngesting = true;
  pathIngestForm.ingestError = null;
  pathIngestForm.ingestResult = null;

  try {
    const params = new URLSearchParams({ path: pathIngestForm.path.trim() });
    const response = await fetch(`${API_BASE_URL}/api/vectorization/ingest-file?${params}`, {
      method: 'POST'
    });

    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`Ingestion failed: ${response.status} ${errorText}`);
    }

    pathIngestForm.ingestResult = await response.json() as VectorizationResult;
    pathIngestForm.path = '';

  } catch (error) {
    pathIngestForm.ingestError = error instanceof Error ? error.message : 'Ingestion failed';
  } finally {
    pathIngestForm.isIngesting = false;
  }
}

function clearResults() {
  fileUploadForm.uploadResult = null;
  fileUploadForm.uploadError = null;
  pathIngestForm.ingestResult = null;
  pathIngestForm.ingestError = null;
}
</script>

<template>
  <div class="vectorization-page">
    <div class="container">
      <!-- Header -->
      <header class="page-header">
        <h1>Document Vectorization</h1>
        <p class="page-subtitle">Upload documents or ingest files for vector processing</p>
      </header>

      <!-- Main Content -->
      <div class="content-grid">
        <!-- File Upload Section -->
        <section class="card upload-card">
          <div class="card-header">
            <h2>📤 Upload File</h2>
            <p>Upload a file directly for vectorization processing</p>
          </div>

          <div class="card-content">
            <div class="form-group">
              <label for="fileInput" class="form-label">Choose File</label>
              <input
                ref="fileInput"
                id="fileInput"
                type="file"
                @change="handleFileSelect"
                class="file-input"
                :disabled="fileUploadForm.isUploading"
              />

              <div v-if="fileUploadForm.file" class="selected-file">
                <span class="file-icon">📄</span>
                <div class="file-info">
                  <div class="file-name">{{ fileUploadForm.file.name }}</div>
                  <div class="file-size">{{ (fileUploadForm.file.size / 1024 / 1024).toFixed(2) }} MB</div>
                </div>
              </div>
            </div>

            <button
              @click="uploadFile"
              :disabled="!fileUploadForm.file || fileUploadForm.isUploading"
              class="btn btn-primary"
            >
              <span v-if="fileUploadForm.isUploading" class="spinner"></span>
              {{ fileUploadForm.isUploading ? 'Uploading...' : 'Upload & Process' }}
            </button>

            <!-- Upload Results -->
            <div v-if="fileUploadForm.uploadError" class="result-box error">
              <div class="result-header">
                <span class="result-icon">❌</span>
                <strong>Upload Failed</strong>
              </div>
              <p>{{ fileUploadForm.uploadError }}</p>
            </div>

            <div v-if="fileUploadForm.uploadResult" class="result-box success">
              <div class="result-header">
                <span class="result-icon">✅</span>
                <strong>Upload Successful</strong>
              </div>
              <pre class="result-data">{{ JSON.stringify(fileUploadForm.uploadResult, null, 2) }}</pre>
            </div>
          </div>
        </section>

        <!-- File Path Ingestion Section -->
        <section class="card ingest-card">
          <div class="card-header">
            <h2>📁 Ingest File Path</h2>
            <p>Process a file from a server path or network location</p>
          </div>

          <div class="card-content">
            <div class="form-group">
              <label for="pathInput" class="form-label">File Path</label>
              <input
                id="pathInput"
                v-model="pathIngestForm.path"
                type="text"
                placeholder="Enter file path (e.g., C:\Documents\file.pdf)"
                class="text-input"
                :disabled="pathIngestForm.isIngesting"
                @keyup.enter="ingestFilePath"
              />
            </div>

            <button
              @click="ingestFilePath"
              :disabled="!pathIngestForm.path.trim() || pathIngestForm.isIngesting"
              class="btn btn-primary"
            >
              <span v-if="pathIngestForm.isIngesting" class="spinner"></span>
              {{ pathIngestForm.isIngesting ? 'Processing...' : 'Ingest File' }}
            </button>

            <!-- Ingestion Results -->
            <div v-if="pathIngestForm.ingestError" class="result-box error">
              <div class="result-header">
                <span class="result-icon">❌</span>
                <strong>Ingestion Failed</strong>
              </div>
              <p>{{ pathIngestForm.ingestError }}</p>
            </div>

            <div v-if="pathIngestForm.ingestResult" class="result-box success">
              <div class="result-header">
                <span class="result-icon">✅</span>
                <strong>Ingestion Successful</strong>
              </div>
              <pre class="result-data">{{ JSON.stringify(pathIngestForm.ingestResult, null, 2) }}</pre>
            </div>
          </div>
        </section>
      </div>

      <!-- Clear Results Button -->
      <div v-if="fileUploadForm.uploadResult || fileUploadForm.uploadError || pathIngestForm.ingestResult || pathIngestForm.ingestError" class="actions">
        <button @click="clearResults" class="btn btn-secondary">
          Clear Results
        </button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.vectorization-page {
  min-height: 100vh;
  background: linear-gradient(135deg, #4f46e5 0%, #7c3aed 100%);
  padding: 2rem 1rem;
}

.container {
  max-width: 1200px;
  margin: 0 auto;
}

.page-header {
  text-align: center;
  margin-bottom: 3rem;
  color: white;
}

.page-header h1 {
  font-size: 2.5rem;
  font-weight: 700;
  margin: 0 0 0.5rem 0;
}

.page-subtitle {
  font-size: 1.1rem;
  opacity: 0.9;
  margin: 0;
}

.content-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(500px, 1fr));
  gap: 2rem;
  margin-bottom: 2rem;
}

.card {
  background: white;
  border-radius: 16px;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.1);
  overflow: hidden;
  transition: transform 0.3s ease;
}

.card:hover {
  transform: translateY(-4px);
}

.card-header {
  background: #f8fafc;
  padding: 2rem;
  border-bottom: 1px solid #e2e8f0;
}

.card-header h2 {
  font-size: 1.5rem;
  font-weight: 600;
  margin: 0 0 0.5rem 0;
  color: #1e293b;
}

.card-header p {
  color: #64748b;
  margin: 0;
}

.card-content {
  padding: 2rem;
}

.form-group {
  margin-bottom: 1.5rem;
}

.form-label {
  display: block;
  font-weight: 600;
  color: #374151;
  margin-bottom: 0.5rem;
}

.file-input {
  width: 100%;
  padding: 0.75rem;
  border: 2px dashed #d1d5db;
  border-radius: 8px;
  background: #f9fafb;
  cursor: pointer;
  transition: all 0.3s ease;
}

.file-input:hover:not(:disabled) {
  border-color: #4f46e5;
  background: #f0f9ff;
}

.file-input:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.selected-file {
  display: flex;
  align-items: center;
  gap: 1rem;
  margin-top: 1rem;
  padding: 1rem;
  background: #f0f9ff;
  border: 1px solid #bfdbfe;
  border-radius: 8px;
}

.file-icon {
  font-size: 1.5rem;
}

.file-info {
  flex: 1;
}

.file-name {
  font-weight: 600;
  color: #1e293b;
}

.file-size {
  font-size: 0.875rem;
  color: #64748b;
}

.text-input {
  width: 100%;
  padding: 0.75rem 1rem;
  border: 1px solid #d1d5db;
  border-radius: 8px;
  font-size: 1rem;
  transition: all 0.3s ease;
}

.text-input:focus {
  outline: none;
  border-color: #4f46e5;
  box-shadow: 0 0 0 3px rgba(79, 70, 229, 0.1);
}

.text-input:disabled {
  background: #f9fafb;
  opacity: 0.5;
}

.btn {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.75rem 1.5rem;
  border: none;
  border-radius: 8px;
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s ease;
  text-decoration: none;
}

.btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.btn-primary {
  background: #4f46e5;
  color: white;
}

.btn-primary:hover:not(:disabled) {
  background: #4338ca;
  transform: translateY(-2px);
}

.btn-secondary {
  background: #6b7280;
  color: white;
}

.btn-secondary:hover:not(:disabled) {
  background: #4b5563;
}

.spinner {
  width: 16px;
  height: 16px;
  border: 2px solid transparent;
  border-top: 2px solid currentColor;
  border-radius: 50%;
  animation: spin 1s linear infinite;
}

@keyframes spin {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}

.result-box {
  margin-top: 1.5rem;
  padding: 1.5rem;
  border-radius: 8px;
  border: 1px solid;
}

.result-box.success {
  background: #f0fdf4;
  border-color: #bbf7d0;
  color: #166534;
}

.result-box.error {
  background: #fef2f2;
  border-color: #fecaca;
  color: #dc2626;
}

.result-header {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin-bottom: 1rem;
  font-weight: 600;
}

.result-icon {
  font-size: 1.25rem;
}

.result-data {
  background: rgba(0, 0, 0, 0.05);
  padding: 1rem;
  border-radius: 6px;
  font-size: 0.875rem;
  overflow-x: auto;
  white-space: pre-wrap;
  word-break: break-all;
}

.actions {
  text-align: center;
}

@media (max-width: 768px) {
  .content-grid {
    grid-template-columns: 1fr;
  }

  .page-header h1 {
    font-size: 2rem;
  }

  .card-header,
  .card-content {
    padding: 1.5rem;
  }
}
</style>
