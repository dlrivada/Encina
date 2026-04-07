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
| Sign_SHA256_MediumPayload  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.007 μs | 0.0151 μs | 0.0090 μs |  1.18 |    0.00 | 0.1068 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.341 μs | 0.0256 μs | 0.0170 μs |  1.28 |    0.01 | 0.1068 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.325 μs | 0.0118 μs | 0.0062 μs |  1.28 |    0.00 | 0.1144 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     44.963 μs | 0.1602 μs | 0.1059 μs | 13.26 |    0.05 | 0.0610 |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      3.391 μs | 0.0149 μs | 0.0099 μs |  1.00 |    0.00 | 0.1068 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      7.105 μs | 0.0282 μs | 0.0186 μs |  2.10 |    0.01 | 0.2136 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      3.802 μs | 0.0068 μs | 0.0041 μs |  1.12 |    0.00 | 0.1106 |   1.81 KB |        1.03 |
|                            |            |                |             |             |              |             |               |           |           |       |         |        |           |             |
| Sign_SHA256_MediumPayload  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  2,889.952 μs |        NA | 0.0000 μs |  0.99 |    0.00 |      - |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  2,871.919 μs |        NA | 0.0000 μs |  0.98 |    0.00 |      - |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  2,942.570 μs |        NA | 0.0000 μs |  1.00 |    0.00 |      - |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  2,905.089 μs |        NA | 0.0000 μs |  0.99 |    0.00 |      - |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  2,930.076 μs |        NA | 0.0000 μs |  1.00 |    0.00 |      - |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 10,220.182 μs |        NA | 0.0000 μs |  3.49 |    0.00 |      - |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  9,776.967 μs |        NA | 0.0000 μs |  3.34 |    0.00 |      - |   1.81 KB |        1.03 |
