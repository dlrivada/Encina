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
| Sign_SHA256_SmallPayload   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      3.528 μs | 0.0155 μs | 0.0081 μs |  1.00 |    0.00 | 0.1068 |   1.77 KB |        1.00 |
| Sign_SHA256_MediumPayload  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.119 μs | 0.0128 μs | 0.0085 μs |  1.17 |    0.00 | 0.1068 |   1.77 KB |        1.00 |
| Sign_SHA256_LargePayload   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     44.979 μs | 0.0701 μs | 0.0417 μs | 12.75 |    0.03 | 0.0610 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.610 μs | 0.0162 μs | 0.0085 μs |  1.31 |    0.00 | 0.1068 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.428 μs | 0.0189 μs | 0.0113 μs |  1.25 |    0.00 | 0.1144 |   1.88 KB |        1.07 |
| Verify_SHA256_SmallPayload | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      3.904 μs | 0.0133 μs | 0.0079 μs |  1.11 |    0.00 | 0.1068 |   1.81 KB |        1.03 |
| SignAndVerify_Roundtrip    | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      7.729 μs | 0.0126 μs | 0.0075 μs |  2.19 |    0.01 | 0.2136 |   3.51 KB |        1.99 |
|                            |            |                |             |             |              |             |               |           |           |       |         |        |           |             |
| Sign_SHA256_SmallPayload   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,118.520 μs |        NA | 0.0000 μs |  1.00 |    0.00 |      - |   1.77 KB |        1.00 |
| Sign_SHA256_MediumPayload  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,119.361 μs |        NA | 0.0000 μs |  1.00 |    0.00 |      - |   1.77 KB |        1.00 |
| Sign_SHA256_LargePayload   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,092.962 μs |        NA | 0.0000 μs |  0.99 |    0.00 |      - |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,118.680 μs |        NA | 0.0000 μs |  1.00 |    0.00 |      - |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,150.169 μs |        NA | 0.0000 μs |  1.01 |    0.00 |      - |   1.88 KB |        1.07 |
| Verify_SHA256_SmallPayload | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 10,034.645 μs |        NA | 0.0000 μs |  3.22 |    0.00 |      - |   1.81 KB |        1.03 |
| SignAndVerify_Roundtrip    | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 10,912.914 μs |        NA | 0.0000 μs |  3.50 |    0.00 |      - |   3.51 KB |        1.99 |
