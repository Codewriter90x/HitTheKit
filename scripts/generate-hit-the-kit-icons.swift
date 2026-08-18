#!/usr/bin/env swift

import AppKit
import Foundation

enum IconGenerationError: Error, CustomStringConvertible {
    case invalidArguments
    case unreadableImage(String)
    case cannotCreateBitmap(Int)
    case cannotEncodePng(String)

    var description: String {
        switch self {
        case .invalidArguments:
            return "Usage: generate-hit-the-kit-icons.swift <source.png> <output-directory>"
        case let .unreadableImage(path):
            return "Cannot read source image at \(path)."
        case let .cannotCreateBitmap(size):
            return "Cannot create a \(size)x\(size) bitmap."
        case let .cannotEncodePng(path):
            return "Cannot encode PNG at \(path)."
        }
    }
}

private let fileManager = FileManager.default

private func ensureDirectory(_ url: URL) throws {
    try fileManager.createDirectory(at: url, withIntermediateDirectories: true)
}

private func loadImage(at url: URL) throws -> NSImage {
    guard let image = NSImage(contentsOf: url) else {
        throw IconGenerationError.unreadableImage(url.path)
    }

    image.size = NSSize(width: image.size.width, height: image.size.height)
    return image
}

private func bitmap(
    size: Int,
    hasAlpha: Bool = true,
    draw: (NSRect) -> Void
) throws -> NSBitmapImageRep {
    guard let representation = NSBitmapImageRep(
        bitmapDataPlanes: nil,
        pixelsWide: size,
        pixelsHigh: size,
        bitsPerSample: 8,
        samplesPerPixel: hasAlpha ? 4 : 3,
        hasAlpha: hasAlpha,
        isPlanar: false,
        colorSpaceName: .deviceRGB,
        bytesPerRow: 0,
        bitsPerPixel: 0
    ) else {
        throw IconGenerationError.cannotCreateBitmap(size)
    }

    representation.size = NSSize(width: size, height: size)
    let context = NSGraphicsContext(bitmapImageRep: representation)
    NSGraphicsContext.saveGraphicsState()
    NSGraphicsContext.current = context
    context?.imageInterpolation = .high
    draw(NSRect(x: 0, y: 0, width: size, height: size))
    context?.flushGraphics()
    NSGraphicsContext.restoreGraphicsState()
    return representation
}

private func pngData(from representation: NSBitmapImageRep, outputPath: String) throws -> Data {
    guard let data = representation.representation(using: .png, properties: [:]) else {
        throw IconGenerationError.cannotEncodePng(outputPath)
    }
    return data
}

private func writePng(_ representation: NSBitmapImageRep, to url: URL) throws {
    try ensureDirectory(url.deletingLastPathComponent())
    try pngData(from: representation, outputPath: url.path).write(to: url, options: .atomic)
}

private func drawImage(_ image: NSImage, in canvas: NSRect, insetRatio: CGFloat) {
    let inset = canvas.width * insetRatio
    let destination = canvas.insetBy(dx: inset, dy: inset)
    image.draw(
        in: destination,
        from: NSRect(origin: .zero, size: image.size),
        operation: .sourceOver,
        fraction: 1,
        respectFlipped: true,
        hints: [.interpolation: NSImageInterpolation.high]
    )
}

private func makeTransparentIcon(source: NSImage, size: Int) throws -> NSBitmapImageRep {
    try bitmap(size: size) { canvas in
        NSColor.clear.setFill()
        canvas.fill()
        drawImage(source, in: canvas, insetRatio: 0)
    }
}

private func makeAppIcon(source: NSImage, size: Int) throws -> NSBitmapImageRep {
    try bitmap(size: size) { canvas in
        NSColor.clear.setFill()
        canvas.fill()

        let outerInset = canvas.width * 0.028
        let cornerRadius = canvas.width * 0.218
        let tileRect = canvas.insetBy(dx: outerInset, dy: outerInset)
        let tile = NSBezierPath(roundedRect: tileRect, xRadius: cornerRadius, yRadius: cornerRadius)

        NSGraphicsContext.saveGraphicsState()
        tile.addClip()
        let background = NSGradient(colorsAndLocations:
            (NSColor(calibratedRed: 0.028, green: 0.034, blue: 0.047, alpha: 1), 0),
            (NSColor(calibratedRed: 0.080, green: 0.041, blue: 0.023, alpha: 1), 0.60),
            (NSColor(calibratedRed: 0.015, green: 0.018, blue: 0.026, alpha: 1), 1)
        )
        background?.draw(in: tile, angle: 90)

        let glow = NSGradient(colorsAndLocations:
            (NSColor(calibratedRed: 1, green: 0.23, blue: 0.015, alpha: 0.22), 0),
            (NSColor(calibratedRed: 1, green: 0.12, blue: 0.005, alpha: 0), 1)
        )
        glow?.draw(
            fromCenter: NSPoint(x: canvas.midX, y: canvas.height * 0.28),
            radius: 0,
            toCenter: NSPoint(x: canvas.midX, y: canvas.height * 0.28),
            radius: canvas.width * 0.53,
            options: []
        )
        NSGraphicsContext.restoreGraphicsState()

        NSColor(calibratedRed: 0.45, green: 0.50, blue: 0.57, alpha: 0.80).setStroke()
        tile.lineWidth = max(1, canvas.width * 0.008)
        tile.stroke()

        let innerBorderRect = tileRect.insetBy(dx: canvas.width * 0.014, dy: canvas.width * 0.014)
        let innerBorder = NSBezierPath(
            roundedRect: innerBorderRect,
            xRadius: max(1, cornerRadius - canvas.width * 0.014),
            yRadius: max(1, cornerRadius - canvas.width * 0.014)
        )
        NSColor(calibratedRed: 1, green: 0.24, blue: 0.02, alpha: 0.46).setStroke()
        innerBorder.lineWidth = max(1, canvas.width * 0.004)
        innerBorder.stroke()

        drawImage(source, in: canvas, insetRatio: 0.073)
    }
}

private func makeOpaquePlatformIcon(source: NSImage, size: Int) throws -> NSBitmapImageRep {
    let composited = try bitmap(size: size) { canvas in
        let background = NSGradient(colorsAndLocations:
            (NSColor(calibratedRed: 0.022, green: 0.027, blue: 0.039, alpha: 1), 0),
            (NSColor(calibratedRed: 0.105, green: 0.040, blue: 0.018, alpha: 1), 0.60),
            (NSColor(calibratedRed: 0.010, green: 0.012, blue: 0.019, alpha: 1), 1)
        )
        background?.draw(in: canvas, angle: 90)

        let glow = NSGradient(colorsAndLocations:
            (NSColor(calibratedRed: 1, green: 0.23, blue: 0.015, alpha: 0.24), 0),
            (NSColor(calibratedRed: 1, green: 0.12, blue: 0.005, alpha: 0), 1)
        )
        glow?.draw(
            fromCenter: NSPoint(x: canvas.midX, y: canvas.height * 0.28),
            radius: 0,
            toCenter: NSPoint(x: canvas.midX, y: canvas.height * 0.28),
            radius: canvas.width * 0.56,
            options: []
        )

        drawImage(source, in: canvas, insetRatio: 0.073)
    }

    guard let sourceImage = composited.cgImage else {
        throw IconGenerationError.cannotCreateBitmap(size)
    }

    let colorSpace = CGColorSpace(name: CGColorSpace.sRGB) ?? CGColorSpaceCreateDeviceRGB()
    let bitmapInfo = CGBitmapInfo.byteOrder32Big.rawValue
        | CGImageAlphaInfo.noneSkipLast.rawValue
    guard let context = CGContext(
        data: nil,
        width: size,
        height: size,
        bitsPerComponent: 8,
        bytesPerRow: size * 4,
        space: colorSpace,
        bitmapInfo: bitmapInfo
    ) else {
        throw IconGenerationError.cannotCreateBitmap(size)
    }

    context.setFillColor(NSColor.black.cgColor)
    context.fill(CGRect(x: 0, y: 0, width: size, height: size))
    context.draw(sourceImage, in: CGRect(x: 0, y: 0, width: size, height: size))
    guard let opaqueImage = context.makeImage() else {
        throw IconGenerationError.cannotCreateBitmap(size)
    }

    return NSBitmapImageRep(cgImage: opaqueImage)
}

private func makePreview(appIcon: NSImage, transparentIcon: NSImage, size: NSSize) throws -> NSBitmapImageRep {
    let width = Int(size.width)
    let height = Int(size.height)
    guard let representation = NSBitmapImageRep(
        bitmapDataPlanes: nil,
        pixelsWide: width,
        pixelsHigh: height,
        bitsPerSample: 8,
        samplesPerPixel: 4,
        hasAlpha: true,
        isPlanar: false,
        colorSpaceName: .deviceRGB,
        bytesPerRow: 0,
        bitsPerPixel: 0
    ) else {
        throw IconGenerationError.cannotCreateBitmap(width)
    }

    representation.size = size
    let context = NSGraphicsContext(bitmapImageRep: representation)
    NSGraphicsContext.saveGraphicsState()
    NSGraphicsContext.current = context
    context?.imageInterpolation = .high

    let canvas = NSRect(origin: .zero, size: size)
    let background = NSGradient(colorsAndLocations:
        (NSColor(calibratedRed: 0.012, green: 0.016, blue: 0.025, alpha: 1), 0),
        (NSColor(calibratedRed: 0.055, green: 0.024, blue: 0.016, alpha: 1), 1)
    )
    background?.draw(in: canvas, angle: 90)

    let titleAttributes: [NSAttributedString.Key: Any] = [
        .font: NSFont.systemFont(ofSize: 42, weight: .bold),
        .foregroundColor: NSColor.white,
        .kern: 2
    ]
    let subtitleAttributes: [NSAttributedString.Key: Any] = [
        .font: NSFont.systemFont(ofSize: 18, weight: .medium),
        .foregroundColor: NSColor(calibratedRed: 0.74, green: 0.79, blue: 0.86, alpha: 1)
    ]
    NSString(string: "HIT THE KIT · APP ICON SYSTEM").draw(
        at: NSPoint(x: 74, y: height - 92),
        withAttributes: titleAttributes
    )
    NSString(string: "Selected flaming drum emblem · production exports").draw(
        at: NSPoint(x: 76, y: height - 124),
        withAttributes: subtitleAttributes
    )

    appIcon.draw(in: NSRect(x: 76, y: 105, width: 520, height: 520))
    transparentIcon.draw(in: NSRect(x: 655, y: 145, width: 430, height: 430))

    let sizes = [256, 128, 64, 32, 16]
    var x: CGFloat = 1135
    for iconSize in sizes {
        let displaySize = CGFloat(iconSize)
        appIcon.draw(in: NSRect(x: x, y: 250, width: displaySize, height: displaySize))
        let label = "\(iconSize) px"
        NSString(string: label).draw(
            at: NSPoint(x: x, y: 215),
            withAttributes: subtitleAttributes
        )
        x += displaySize + 38
    }

    NSGraphicsContext.restoreGraphicsState()
    return representation
}

private func littleEndianBytes<T: FixedWidthInteger>(_ value: T) -> Data {
    var littleEndian = value.littleEndian
    return Data(bytes: &littleEndian, count: MemoryLayout<T>.size)
}

private func writeIco(pngs: [(size: Int, data: Data)], to url: URL) throws {
    var output = Data()
    output.append(littleEndianBytes(UInt16(0)))
    output.append(littleEndianBytes(UInt16(1)))
    output.append(littleEndianBytes(UInt16(pngs.count)))

    let directorySize = 6 + (16 * pngs.count)
    var offset = directorySize
    for entry in pngs {
        output.append(UInt8(entry.size >= 256 ? 0 : entry.size))
        output.append(UInt8(entry.size >= 256 ? 0 : entry.size))
        output.append(UInt8(0))
        output.append(UInt8(0))
        output.append(littleEndianBytes(UInt16(1)))
        output.append(littleEndianBytes(UInt16(32)))
        output.append(littleEndianBytes(UInt32(entry.data.count)))
        output.append(littleEndianBytes(UInt32(offset)))
        offset += entry.data.count
    }

    for entry in pngs {
        output.append(entry.data)
    }

    try ensureDirectory(url.deletingLastPathComponent())
    try output.write(to: url, options: .atomic)
}

private func bigEndianBytes<T: FixedWidthInteger>(_ value: T) -> Data {
    var bigEndian = value.bigEndian
    return Data(bytes: &bigEndian, count: MemoryLayout<T>.size)
}

private func writeIcns(pngs: [(type: String, data: Data)], to url: URL) throws {
    let payloadSize = pngs.reduce(0) { total, entry in
        total + 8 + entry.data.count
    }

    var output = Data("icns".utf8)
    output.append(bigEndianBytes(UInt32(8 + payloadSize)))
    for entry in pngs {
        precondition(entry.type.utf8.count == 4)
        output.append(Data(entry.type.utf8))
        output.append(bigEndianBytes(UInt32(8 + entry.data.count)))
        output.append(entry.data)
    }

    try ensureDirectory(url.deletingLastPathComponent())
    try output.write(to: url, options: .atomic)
}

private func main() throws {
    guard CommandLine.arguments.count == 3 else {
        throw IconGenerationError.invalidArguments
    }

    let sourceUrl = URL(fileURLWithPath: CommandLine.arguments[1]).standardizedFileURL
    let outputRoot = URL(fileURLWithPath: CommandLine.arguments[2]).standardizedFileURL
    let source = try loadImage(at: sourceUrl)

    let masterDirectory = outputRoot.appendingPathComponent("master")
    let transparentDirectory = outputRoot.appendingPathComponent("transparent-png")
    let appDirectory = outputRoot.appendingPathComponent("app-png")
    let macDirectory = outputRoot.appendingPathComponent("macos")
    let windowsDirectory = outputRoot.appendingPathComponent("windows")
    let webDirectory = outputRoot.appendingPathComponent("web")
    let mobileDirectory = outputRoot.appendingPathComponent("mobile")
    let socialDirectory = outputRoot.appendingPathComponent("social")
    let previewDirectory = outputRoot.appendingPathComponent("preview")
    let iconsetDirectory = macDirectory.appendingPathComponent("HitTheKit.iconset")

    for directory in [masterDirectory, transparentDirectory, appDirectory, macDirectory, windowsDirectory, webDirectory, mobileDirectory, socialDirectory, previewDirectory, iconsetDirectory] {
        try ensureDirectory(directory)
    }

    try Data(contentsOf: sourceUrl).write(
        to: masterDirectory.appendingPathComponent("HitTheKit-Icon-Source-1254.png"),
        options: .atomic
    )

    let transparentSizes = [1024, 512, 256, 128, 64, 32]
    for size in transparentSizes {
        let icon = try makeTransparentIcon(source: source, size: size)
        try writePng(icon, to: transparentDirectory.appendingPathComponent("HitTheKit-Logo-Transparent-\(size).png"))
    }

    let appSizes = [1024, 512, 256, 128, 64, 48, 32, 24, 16]
    var appPngs: [Int: Data] = [:]
    for size in appSizes {
        let icon = try makeAppIcon(source: source, size: size)
        let output = appDirectory.appendingPathComponent("HitTheKit-AppIcon-\(size).png")
        try writePng(icon, to: output)
        appPngs[size] = try Data(contentsOf: output)
    }

    let iconsetFiles: [(name: String, size: Int)] = [
        ("icon_16x16.png", 16),
        ("icon_16x16@2x.png", 32),
        ("icon_32x32.png", 32),
        ("icon_32x32@2x.png", 64),
        ("icon_128x128.png", 128),
        ("icon_128x128@2x.png", 256),
        ("icon_256x256.png", 256),
        ("icon_256x256@2x.png", 512),
        ("icon_512x512.png", 512),
        ("icon_512x512@2x.png", 1024)
    ]
    for item in iconsetFiles {
        guard let data = appPngs[item.size] else {
            throw IconGenerationError.cannotEncodePng(item.name)
        }
        try data.write(to: iconsetDirectory.appendingPathComponent(item.name), options: .atomic)
    }
    let icnsEntries: [(type: String, size: Int)] = [
        ("icp4", 16),
        ("icp5", 32),
        ("icp6", 64),
        ("ic07", 128),
        ("ic08", 256),
        ("ic09", 512),
        ("ic10", 1024)
    ]
    let icnsPngs = try icnsEntries.map { entry -> (type: String, data: Data) in
        guard let data = appPngs[entry.size] else {
            throw IconGenerationError.cannotEncodePng("ICNS \(entry.size)")
        }
        return (entry.type, data)
    }
    try writeIcns(
        pngs: icnsPngs,
        to: macDirectory.appendingPathComponent("HitTheKit.icns")
    )

    let icoSizes = [16, 24, 32, 48, 64, 128, 256]
    let icoPngs = try icoSizes.map { size -> (size: Int, data: Data) in
        guard let data = appPngs[size] else {
            throw IconGenerationError.cannotEncodePng("ICO \(size)")
        }
        return (size, data)
    }
    try writeIco(pngs: icoPngs, to: windowsDirectory.appendingPathComponent("HitTheKit.ico"))

    for size in [16, 32, 48] {
        guard let data = appPngs[size] else {
            throw IconGenerationError.cannotEncodePng("favicon \(size)")
        }
        try data.write(to: webDirectory.appendingPathComponent("favicon-\(size).png"), options: .atomic)
    }

    let opaqueExports: [(name: String, size: Int)] = [
        ("HitTheKit-iOS-AppIcon-1024.png", 1024),
        ("HitTheKit-Android-Legacy-512.png", 512),
        ("apple-touch-icon-180.png", 180),
        ("pwa-icon-192.png", 192),
        ("pwa-icon-512.png", 512)
    ]
    for export in opaqueExports {
        let icon = try makeOpaquePlatformIcon(source: source, size: export.size)
        let directory = export.name.hasPrefix("HitTheKit-") ? mobileDirectory : webDirectory
        try writePng(icon, to: directory.appendingPathComponent(export.name))
    }

    try appPngs[512]!.write(
        to: socialDirectory.appendingPathComponent("HitTheKit-GitHub-Avatar-512.png"),
        options: .atomic
    )
    try appPngs[1024]!.write(
        to: socialDirectory.appendingPathComponent("HitTheKit-Release-Artwork-1024.png"),
        options: .atomic
    )

    let appPreviewImage = NSImage(data: appPngs[1024]!)!
    let transparentPreviewData = try Data(contentsOf: transparentDirectory.appendingPathComponent("HitTheKit-Logo-Transparent-1024.png"))
    let transparentPreviewImage = NSImage(data: transparentPreviewData)!
    let preview = try makePreview(
        appIcon: appPreviewImage,
        transparentIcon: transparentPreviewImage,
        size: NSSize(width: 1920, height: 810)
    )
    try writePng(preview, to: previewDirectory.appendingPathComponent("HitTheKit-Icon-Family-Preview.png"))
}

do {
    try main()
} catch {
    FileHandle.standardError.write(Data("error: \(error)\n".utf8))
    exit(1)
}
