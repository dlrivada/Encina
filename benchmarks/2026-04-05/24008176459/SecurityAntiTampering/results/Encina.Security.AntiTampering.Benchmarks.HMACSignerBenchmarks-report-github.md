```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                     | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean          | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |--------------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Sign_SHA256_SmallPayload   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      3.368 μs | 0.0107 μs | 0.0071 μs |  1.00 |    0.00 | 0.1068 |   1.77 KB |        1.00 |
| Sign_SHA256_MediumPayload  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      3.990 μs | 0.0186 μs | 0.0111 μs |  1.18 |    0.00 | 0.1068 |   1.77 KB |        1.00 |
| Sign_SHA256_LargePayload   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     44.799 μs | 0.1224 μs | 0.0728 μs | 13.30 |    0.03 | 0.0610 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.487 μs | 0.0136 μs | 0.0090 μs |  1.33 |    0.00 | 0.1068 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.276 μs | 0.0137 μs | 0.0091 μs |  1.27 |    0.00 | 0.1144 |   1.88 KB |        1.07 |
| Verify_SHA256_SmallPayload | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      3.968 μs | 0.0288 μs | 0.0171 μs |  1.18 |    0.01 | 0.1068 |   1.81 KB |        1.03 |
| SignAndVerify_Roundtrip    | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      7.421 μs | 0.0136 μs | 0.0090 μs |  2.20 |    0.01 | 0.2136 |   3.51 KB |        1.99 |
|                            |            |                |             |             |              |             |               |           |           |       |         |        |           |             |
| Sign_SHA256_SmallPayload   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  2,811.419 μs |        NA | 0.0000 μs |  1.00 |    0.00 |      - |   1.77 KB |        1.00 |
| Sign_SHA256_MediumPayload  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  2,825.085 μs |        NA | 0.0000 μs |  1.00 |    0.00 |      - |   1.77 KB |        1.00 |
| Sign_SHA256_LargePayload   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  2,908.500 μs |        NA | 0.0000 μs |  1.03 |    0.00 |      - |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  2,856.373 μs |        NA | 0.0000 μs |  1.02 |    0.00 |      - |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  2,910.374 μs |        NA | 0.0000 μs |  1.04 |    0.00 |      - |   1.88 KB |        1.07 |
| Verify_SHA256_SmallPayload | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  9,511.603 μs |        NA | 0.0000 μs |  3.38 |    0.00 |      - |   1.81 KB |        1.03 |
| SignAndVerify_Roundtrip    | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 10,040.857 μs |        NA | 0.0000 μs |  3.57 |    0.00 |      - |   3.51 KB |        1.99 |
