```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.77GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3


```
| Method                  | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |----------- |--------------- |------------ |------------ |---------:|----------:|----------:|---------:|------:|--------:|-------:|-------:|----------:|------------:|
| EncryptString_Medium    | Job-YFEFPZ | 10             | Default     | 3           | 4.260 μs | 0.0531 μs | 0.0351 μs | 4.272 μs |  0.99 |    0.01 | 0.0687 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | Job-YFEFPZ | 10             | Default     | 3           | 7.319 μs | 0.0388 μs | 0.0257 μs | 7.306 μs |  1.70 |    0.01 | 0.0381 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | Job-YFEFPZ | 10             | Default     | 3           | 3.936 μs | 0.0212 μs | 0.0111 μs | 3.933 μs |  0.92 |    0.00 | 0.0229 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | Job-YFEFPZ | 10             | Default     | 3           | 4.538 μs | 0.0545 μs | 0.0361 μs | 4.538 μs |  1.06 |    0.01 | 0.0687 |      - |    1168 B |        2.61 |
| DecryptString_Short     | Job-YFEFPZ | 10             | Default     | 3           | 2.675 μs | 0.0166 μs | 0.0099 μs | 2.671 μs |  0.62 |    0.00 | 0.0191 |      - |     320 B |        0.71 |
| EncryptString_Long      | Job-YFEFPZ | 10             | Default     | 3           | 6.770 μs | 0.0453 μs | 0.0300 μs | 6.768 μs |  1.58 |    0.01 | 0.7553 | 0.0305 |   12688 B |       28.32 |
| EncryptString_Short     | Job-YFEFPZ | 10             | Default     | 3           | 4.296 μs | 0.0247 μs | 0.0163 μs | 4.296 μs |  1.00 |    0.01 | 0.0229 |      - |     448 B |        1.00 |
|                         |            |                |             |             |          |           |           |          |       |         |        |        |           |             |
| EncryptString_Medium    | MediumRun  | 15             | 2           | 10          | 4.263 μs | 0.0197 μs | 0.0283 μs | 4.269 μs |  1.03 |    0.04 | 0.0687 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | MediumRun  | 15             | 2           | 10          | 6.686 μs | 0.0299 μs | 0.0439 μs | 6.679 μs |  1.62 |    0.07 | 0.0381 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | MediumRun  | 15             | 2           | 10          | 4.166 μs | 0.0958 μs | 0.1373 μs | 4.177 μs |  1.01 |    0.05 | 0.0229 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | MediumRun  | 15             | 2           | 10          | 4.344 μs | 0.1209 μs | 0.1772 μs | 4.214 μs |  1.05 |    0.06 | 0.0687 |      - |    1168 B |        2.61 |
| DecryptString_Short     | MediumRun  | 15             | 2           | 10          | 2.756 μs | 0.0988 μs | 0.1384 μs | 2.644 μs |  0.67 |    0.04 | 0.0191 |      - |     320 B |        0.71 |
| EncryptString_Long      | MediumRun  | 15             | 2           | 10          | 6.868 μs | 0.1032 μs | 0.1479 μs | 6.919 μs |  1.67 |    0.08 | 0.7553 | 0.0305 |   12688 B |       28.32 |
| EncryptString_Short     | MediumRun  | 15             | 2           | 10          | 4.126 μs | 0.1227 μs | 0.1680 μs | 4.131 μs |  1.00 |    0.06 | 0.0229 |      - |     448 B |        1.00 |
