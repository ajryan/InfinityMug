import { Component, Inject, ViewChild } from '@angular/core'
import { HttpClient } from '@angular/common/http'
import { Subscription, Subject } from 'rxjs'
import { ImageCropperComponent } from '../image-cropper/component/image-cropper.component'
import { ImageCroppedEvent } from '../image-cropper/interfaces/image-cropped-event.interface'
import { FormGroup, FormBuilder, FormControl } from '@angular/forms'
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
  readonly parametersForm: FormGroup
  readonly webcamCapture$ = new Subject()
  imageChangedEvent?: Event
  imageBase64?: string
  inputImage?: File
  lastCropEvent?: ImageCroppedEvent
  drosteImage?: any = ''
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
    this.imageSource = new FormControl('upload')
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

  onImageCropped(event: ImageCroppedEvent): void {
    this.lastCropEvent = event
    this.getNewPreview()
  }

  private getNewPreview(): void {
    if (!this.lastCropEvent || (!this.inputImage && !this.imageBase64)) {
      return
    }

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
        }
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
