```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.79GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                     | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean          | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |--------------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Sign_SHA256_MediumPayload  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.044 μs | 0.0130 μs | 0.0086 μs |  1.18 |    0.00 | 0.1068 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.513 μs | 0.0103 μs | 0.0068 μs |  1.32 |    0.00 | 0.1068 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.370 μs | 0.0094 μs | 0.0056 μs |  1.28 |    0.00 | 0.1144 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     44.759 μs | 0.1341 μs | 0.0887 μs | 13.09 |    0.04 | 0.0610 |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      3.420 μs | 0.0151 μs | 0.0090 μs |  1.00 |    0.00 | 0.1068 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      7.523 μs | 0.0177 μs | 0.0105 μs |  2.20 |    0.01 | 0.2136 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      3.987 μs | 0.0145 μs | 0.0096 μs |  1.17 |    0.00 | 0.1068 |   1.81 KB |        1.03 |
|                            |            |                |             |             |              |             |               |           |           |       |         |        |           |             |
| Sign_SHA256_MediumPayload  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  2,939.918 μs |        NA | 0.0000 μs |  0.99 |    0.00 |      - |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  2,964.044 μs |        NA | 0.0000 μs |  1.00 |    0.00 |      - |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  2,940.179 μs |        NA | 0.0000 μs |  0.99 |    0.00 |      - |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  2,987.479 μs |        NA | 0.0000 μs |  1.01 |    0.00 |      - |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  2,967.452 μs |        NA | 0.0000 μs |  1.00 |    0.00 |      - |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 10,242.434 μs |        NA | 0.0000 μs |  3.45 |    0.00 |      - |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  9,548.007 μs |        NA | 0.0000 μs |  3.22 |    0.00 |      - |   1.81 KB |        1.03 |
