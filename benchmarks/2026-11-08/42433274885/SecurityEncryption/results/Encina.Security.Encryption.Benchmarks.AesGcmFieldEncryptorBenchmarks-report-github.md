```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4


```
| Method                  | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |----------- |--------------- |------------ |------------ |---------:|----------:|----------:|---------:|------:|--------:|-------:|-------:|----------:|------------:|
| EncryptString_Medium    | Job-YFEFPZ | 10             | Default     | 3           | 3.297 μs | 0.0567 μs | 0.0375 μs | 3.291 μs |  1.07 |    0.01 | 0.0687 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | Job-YFEFPZ | 10             | Default     | 3           | 5.123 μs | 0.0208 μs | 0.0124 μs | 5.119 μs |  1.66 |    0.01 | 0.0381 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | Job-YFEFPZ | 10             | Default     | 3           | 3.286 μs | 0.0331 μs | 0.0219 μs | 3.282 μs |  1.06 |    0.01 | 0.0267 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | Job-YFEFPZ | 10             | Default     | 3           | 3.255 μs | 0.0327 μs | 0.0216 μs | 3.253 μs |  1.05 |    0.01 | 0.0687 |      - |    1168 B |        2.61 |
| DecryptString_Short     | Job-YFEFPZ | 10             | Default     | 3           | 2.007 μs | 0.0091 μs | 0.0047 μs | 2.006 μs |  0.65 |    0.00 | 0.0191 |      - |     320 B |        0.71 |
| EncryptString_Long      | Job-YFEFPZ | 10             | Default     | 3           | 5.335 μs | 0.0497 μs | 0.0329 μs | 5.351 μs |  1.73 |    0.01 | 0.7553 | 0.0305 |   12688 B |       28.32 |
| EncryptString_Short     | Job-YFEFPZ | 10             | Default     | 3           | 3.088 μs | 0.0306 μs | 0.0202 μs | 3.092 μs |  1.00 |    0.01 | 0.0267 |      - |     448 B |        1.00 |
|                         |            |                |             |             |          |           |           |          |       |         |        |        |           |             |
| EncryptString_Medium    | MediumRun  | 15             | 2           | 10          | 3.385 μs | 0.0797 μs | 0.1090 μs | 3.462 μs |  1.07 |    0.04 | 0.0687 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | MediumRun  | 15             | 2           | 10          | 5.320 μs | 0.1835 μs | 0.2512 μs | 5.319 μs |  1.68 |    0.09 | 0.0381 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | MediumRun  | 15             | 2           | 10          | 3.156 μs | 0.0857 μs | 0.1256 μs | 3.258 μs |  0.99 |    0.05 | 0.0267 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | MediumRun  | 15             | 2           | 10          | 3.341 μs | 0.0972 μs | 0.1394 μs | 3.288 μs |  1.05 |    0.05 | 0.0687 |      - |    1168 B |        2.61 |
| DecryptString_Short     | MediumRun  | 15             | 2           | 10          | 2.124 μs | 0.0910 μs | 0.1276 μs | 2.243 μs |  0.67 |    0.04 | 0.0191 |      - |     320 B |        0.71 |
| EncryptString_Long      | MediumRun  | 15             | 2           | 10          | 5.034 μs | 0.1323 μs | 0.1854 μs | 5.095 μs |  1.59 |    0.07 | 0.7553 | 0.0305 |   12688 B |       28.32 |
| EncryptString_Short     | MediumRun  | 15             | 2           | 10          | 3.176 μs | 0.0575 μs | 0.0788 μs | 3.146 μs |  1.00 |    0.03 | 0.0267 |      - |     448 B |        1.00 |
