import { Component, Inject, ViewChild } from '@angular/core'
import { HttpClient } from '@angular/common/http'
import { Subscription } from 'rxjs'
import { ImageCropperComponent } from '../image-cropper/component/image-cropper.component'
import { ImageCroppedEvent } from '../image-cropper/interfaces/image-cropped-event.interface'
import { FormGroup, FormBuilder } from '@angular/forms'

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss']
})
export class HomeComponent {
  @ViewChild(ImageCropperComponent, { static: false })
  imageCropper: ImageCropperComponent

  readonly parametersForm: FormGroup
  imageChangedEvent?: Event
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
    this.parametersForm.valueChanges.subscribe(() => this.getNewPreview())
  }

  onImageChanged(event: Event): void {
    this.imageChangedEvent = event

    const inputElement = event.target as HTMLInputElement
    this.inputImage = inputElement.files[0]
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
      this.imageCropper.maxSize.width.toString()
    )
    formData.append('rotationDegrees', this.parametersForm.value.rotation.toString())
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
}
