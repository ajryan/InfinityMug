import { Component, Inject, ViewChild } from '@angular/core'
import { ImageCroppedEvent, ImageCropperComponent } from 'ngx-image-cropper'
import { HttpClient } from '@angular/common/http'
import { Subscription } from 'rxjs'

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss']
})
export class HomeComponent {
  @ViewChild(ImageCropperComponent, { static: false })
  imageCropper: ImageCropperComponent

  imageChangedEvent?: Event
  inputImage?: File
  lastCropEvent?: ImageCroppedEvent
  drosteImage?: any
  rotationDegrees = -18
  private renderSub: Subscription

  constructor(
    @Inject('BASE_URL') private readonly baseUrl: string,
    private readonly http: HttpClient
  ) {}

  get imageAspectRatio(): string | undefined {
    return (
      this.lastCropEvent &&
      `${this.lastCropEvent.height} / ${this.lastCropEvent.width}`
    )
  }

  onImageChanged(event: Event): void {
    this.imageChangedEvent = event

    const inputElement = event.target as HTMLInputElement
    this.inputImage = inputElement.files[0]
  }

  onRotationChange(value: number): void {
    this.rotationDegrees = value
    this.getNewPreview()
  }

  onImageCropped(event: ImageCroppedEvent): void {
    this.lastCropEvent = event
    this.getNewPreview()
  }

  private getNewPreview(): void {
    if (!this.lastCropEvent) {
      return
    }

    const formData = new FormData()
    formData.append('image', this.inputImage, 'input.png')
    formData.append('crop', JSON.stringify(this.lastCropEvent.cropperPosition))
    formData.append(
      'displayedWidth',
      this.imageCropper['maxSize'].width.toString()
    )
    formData.append('rotationDegrees', this.rotationDegrees.toString())

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
}
