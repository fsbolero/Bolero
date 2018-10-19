module MiniBlazor.Json

open System
open System.IO

/// Defines a JSON serialized name for a property or field when it differs from the F# name.
[<Sealed>]
type NameAttribute =
    inherit Attribute

    /// Defines a JSON serialized name for a property or field when it differs from the F# name.
    new : name: string -> NameAttribute

/// Defines the format used to de/serialize a DateTime field or union case argument.
/// The default is "o" (ISO 8601 round-trip format).
[<Sealed>]
type DateTimeFormatAttribute =
    inherit Attribute

    /// Defines the format used to de/serialize a DateTime record or object field.
    /// The default is "o" (ISO 8601 round-trip format).
    new : format: string -> DateTimeFormatAttribute

    /// Defines the format used to de/serialize a DateTime union case argument.
    /// The default is "o" (ISO 8601 round-trip format).
    new : argumentName: string * format: string -> DateTimeFormatAttribute

/// Declares that when de/serializing this union,
/// its fields must be tagged by their name rather than "$0" ... "$n".
/// Also determines how the cases are distinguished, instead of the default "$": <integer>.
[<Sealed>]
type NamedUnionCasesAttribute =
    inherit Attribute

    /// The case is determined by a field named `discriminatorName`,
    /// which stores the name of the case.
    new : discriminatorName: string -> NamedUnionCasesAttribute

    /// The case is inferred from the field names. Every case must have at least one
    /// non-option-typed field whose name is unique across all cases of this union.
    new : unit -> NamedUnionCasesAttribute

/// Declares that a union case should be de/serialized as a constant.
[<Sealed>]
type ConstantAttribute =
    inherit Attribute

    /// Declares that a union case should be de/serialized as a boolean constant.
    new : value: bool -> ConstantAttribute

    /// Declares that a union case should be de/serialized as an integer constant.
    new : value: int -> ConstantAttribute

    /// Declares that a union case should be de/serialized as a float constant.
    new : value: float -> ConstantAttribute

    /// Declares that a union case should be de/serialized as a string constant.
    new : value: string -> ConstantAttribute

/// Represents a JSON value.
type Value =
    | Null
    | True
    | False
    | Number of string
    | String of string
    | Array of list<Value>
    | Object of list<string * Value>

/// Plain JSON serialization and deserialization from and to Value.
module Raw =

    /// Thrown when text being read from the text reader is not valid JSON.
    exception ReadException

    /// Thrown when the value being written is not valid JSON.
    exception WriteException

    /// Reads raw JSON. Throws ReadError.
    val Read : TextReader -> Value

    /// Parses a JSON string. Throws ReadError.
    val Parse : string -> Value

    /// Writes raw JSON. Throws WriteError.
    val Write : TextWriter -> Value -> unit

    /// Converts JSON to a string. Throws WriteError.
    val Stringify : Value -> string

/// Thrown when the decoder fails to reconstruct a value from JSON.
exception DecoderException of value:Value * typ:Type

/// Thrown when the encoder is given a value it cannot encode.
exception EncoderException

/// Thrown when no decoder can be derived for a given type.
exception NoDecoderException of typ:Type

/// Thrown when no encoder can be derived for a given type.
exception NoEncoderException of typ:Type

/// Reads a JSON value.
val Read<'T> : TextReader -> 'T

/// Deserializes a JSON value.
val Deserialize<'T> : string -> 'T

/// Writes a value as JSON.
val Write<'T> : TextWriter -> 'T -> unit

/// Serializes a value to JSON.
val Serialize<'T> : 'T -> string

/// A JSON decoder from a given type.
type Decoder<'T> = Value -> 'T

/// A JSON decoder from a given type.
type Encoder<'T> = 'T -> Value

/// Get the decoder for a type.
val GetDecoder : Type -> Decoder<obj>

/// Get the encoder for a type.
val GetEncoder : Type -> Encoder<obj>
