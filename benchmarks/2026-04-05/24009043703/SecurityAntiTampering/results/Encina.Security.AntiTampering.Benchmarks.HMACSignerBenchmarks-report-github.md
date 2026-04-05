```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4


```
| Method                     | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean          | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |--------------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Sign_SHA256_SmallPayload   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.062 μs | 0.0141 μs | 0.0084 μs |  1.00 |    0.00 | 0.0687 |   1.77 KB |        1.00 |
| Sign_SHA256_MediumPayload  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      5.240 μs | 0.0167 μs | 0.0088 μs |  1.29 |    0.00 | 0.0687 |   1.77 KB |        1.00 |
| Sign_SHA256_LargePayload   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     80.190 μs | 0.0490 μs | 0.0292 μs | 19.74 |    0.04 |      - |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      5.022 μs | 0.0122 μs | 0.0080 μs |  1.24 |    0.00 | 0.0687 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.956 μs | 0.0145 μs | 0.0087 μs |  1.22 |    0.00 | 0.0763 |   1.88 KB |        1.07 |
| Verify_SHA256_SmallPayload | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.443 μs | 0.0056 μs | 0.0029 μs |  1.09 |    0.00 | 0.0687 |   1.81 KB |        1.03 |
| SignAndVerify_Roundtrip    | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      8.564 μs | 0.0146 μs | 0.0087 μs |  2.11 |    0.00 | 0.1373 |   3.51 KB |        1.99 |
|                            |            |                |             |             |              |             |               |           |           |       |         |        |           |             |
| Sign_SHA256_SmallPayload   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,000.400 μs |        NA | 0.0000 μs |  1.00 |    0.00 |      - |   1.77 KB |        1.00 |
| Sign_SHA256_MediumPayload  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  2,936.890 μs |        NA | 0.0000 μs |  0.98 |    0.00 |      - |   1.77 KB |        1.00 |
| Sign_SHA256_LargePayload   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,175.836 μs |        NA | 0.0000 μs |  1.06 |    0.00 |      - |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,064.986 μs |        NA | 0.0000 μs |  1.02 |    0.00 |      - |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,220.584 μs |        NA | 0.0000 μs |  1.07 |    0.00 |      - |   1.88 KB |        1.07 |
| Verify_SHA256_SmallPayload | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  9,840.989 μs |        NA | 0.0000 μs |  3.28 |    0.00 |      - |   1.81 KB |        1.03 |
| SignAndVerify_Roundtrip    | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 10,598.573 μs |        NA | 0.0000 μs |  3.53 |    0.00 |      - |   3.51 KB |        1.99 |
