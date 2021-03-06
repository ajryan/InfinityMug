import { Component, Inject, ViewChild } from '@angular/core'
import { HttpClient } from '@angular/common/http'
import { Subscription, Subject, BehaviorSubject } from 'rxjs'
import { ImageCropperComponent } from '../image-cropper/component/image-cropper.component'
import { ImageCroppedEvent } from '../image-cropper/interfaces/image-cropped-event.interface'
import { FormGroup, FormBuilder, FormControl, Validators } from '@angular/forms'
import { WebcamImage } from 'ngx-webcam'

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss']
})
export class HomeComponent {
  @ViewChild(ImageCropperComponent, { static: false })
  imageCropper: ImageCropperComponent

  readonly imageSource: FormControl
  readonly userEmail: FormControl
  readonly parametersForm: FormGroup
  readonly webcamCapture$ = new Subject()
  readonly processing$ = new BehaviorSubject(false)
  imageChangedEvent?: Event
  imageBase64?: string
  inputImage?: File
  lastCropEvent?: ImageCroppedEvent
  drosteImage?: string | ArrayBuffer = ''
  roundCropper = true

  private renderSub: Subscription

  constructor(
    fb: FormBuilder,
    @Inject('BASE_URL') private readonly baseUrl: string,
    private readonly http: HttpClient
  ) {
    this.parametersForm = fb.group({
      rotation: -18,
      roundCropper: true
    })
    this.imageSource = new FormControl('camera')
    this.userEmail = new FormControl(
      undefined,
      Validators.compose([Validators.email, Validators.required])
    )
    this.parametersForm.valueChanges.subscribe(() => this.getNewPreview())
  }

  onWebcamImageCapture(event: WebcamImage): void {
    this.imageChangedEvent = undefined
    this.imageBase64 = event.imageAsDataUrl
  }

  onImageChanged(event: Event): void {
    this.imageBase64 = undefined
    this.imageChangedEvent = event

    const inputElement = event.target as HTMLInputElement
    this.inputImage = inputElement.files[0]
  }

  onStartOver(): void {
    this.imageBase64 = undefined
    this.imageChangedEvent = undefined
    this.drosteImage = undefined
  }

  onImageCropped(event: ImageCroppedEvent): void {
    this.lastCropEvent = event
    this.getNewPreview()
  }

  onMakeMug(): void {
    if (this.userEmail.valid) {
      this.processing$.next(true)
      this.http
        .post(`${this.baseUrl}droste/make`, {
          imageBase64: this.drosteImage,
          userEmail: this.userEmail.value
        })
        .subscribe({
          next: () => {
            this.processing$.next(false)
            alert('done')
          },
          error: (err) => {
            console.error(err)
            this.processing$.next(false)
            alert('failed')
          }
        })
    }
  }

  private getNewPreview(): void {
    if (!this.lastCropEvent || (!this.inputImage && !this.imageBase64)) {
      return
    }

    this.processing$.next(true)
    const formData = new FormData()

    if (this.inputImage) {
      formData.append('image', this.inputImage, 'input.png')
    } else {
      formData.append(
        'image',
        this.dataUriToBlob(this.imageBase64),
        'input.jpg'
      )
    }
    formData.append('crop', JSON.stringify(this.lastCropEvent.cropperPosition))
    formData.append(
      'displayedWidth',
      this.imageCropper.maxSize.width.toString()
    )
    formData.append(
      'rotationDegrees',
      this.parametersForm.value.rotation.toString()
    )
    formData.append('round', this.parametersForm.value.roundCropper.toString())

    if (this.renderSub) {
      this.renderSub.unsubscribe()
    }
    this.renderSub = this.http
      .post(`${this.baseUrl}droste`, formData, { responseType: 'blob' })
      .subscribe(
        res => {
          const reader = new FileReader()
          reader.addEventListener(
            'load',
            () => (this.drosteImage = reader.result)
          )
          reader.readAsDataURL(res)
        },
        err => {
          console.log(err)
        },
        () => this.processing$.next(false)
      )
  }

  private dataUriToBlob(dataUri: string) {
    const binary = atob(dataUri.split(',')[1])
    const array = []
    for (let i = 0; i < binary.length; i++) {
      array.push(binary.charCodeAt(i))
    }
    return new Blob([new Uint8Array(array)], { type: 'image/jpeg' })
  }
}
