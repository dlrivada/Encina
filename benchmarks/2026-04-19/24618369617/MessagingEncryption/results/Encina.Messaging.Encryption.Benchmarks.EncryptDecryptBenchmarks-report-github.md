```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.202
  [Host]     : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v4


```
| Method          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     | 3           |  5.359 μs | 0.0185 μs | 0.0110 μs |  5.359 μs |  1.10 |    0.00 | 0.1450 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | 3           | 24.047 μs | 0.6078 μs | 0.4020 μs | 23.837 μs |  4.95 |    0.08 | 7.8125 | 1.7090 |  197226 B |      267.97 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     | 3           |  3.644 μs | 0.0087 μs | 0.0052 μs |  3.646 μs |  0.75 |    0.00 | 0.0954 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | 3           | 16.894 μs | 0.2231 μs | 0.1328 μs | 16.936 μs |  3.48 |    0.03 | 5.2185 | 0.6409 |  131441 B |      178.59 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     | 3           |  3.268 μs | 0.0061 μs | 0.0036 μs |  3.267 μs |  0.67 |    0.00 | 0.0153 |      - |     448 B |        0.61 |
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     | 3           |  4.855 μs | 0.0117 μs | 0.0077 μs |  4.856 μs |  1.00 |    0.00 | 0.0229 |      - |     736 B |        1.00 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     | 3           |  8.277 μs | 0.0184 μs | 0.0122 μs |  8.280 μs |  1.70 |    0.00 | 0.0458 |      - |    1184 B |        1.61 |
|                 |            |                |             |             |           |           |           |           |       |         |        |        |           |             |
| Encrypt_Medium  | MediumRun  | 15             | 2           | 10          |  5.352 μs | 0.0198 μs | 0.0296 μs |  5.358 μs |  1.12 |    0.01 | 0.1450 |      - |    3688 B |        5.01 |
| Encrypt_Large   | MediumRun  | 15             | 2           | 10          | 23.924 μs | 0.4440 μs | 0.6645 μs | 23.855 μs |  5.00 |    0.14 | 7.8125 | 1.7090 |  197226 B |      267.97 |
| Decrypt_Medium  | MediumRun  | 15             | 2           | 10          |  3.692 μs | 0.0565 μs | 0.0810 μs |  3.751 μs |  0.77 |    0.02 | 0.0954 |      - |    2416 B |        3.28 |
| Decrypt_Large   | MediumRun  | 15             | 2           | 10          | 17.564 μs | 0.4616 μs | 0.6908 μs | 17.546 μs |  3.67 |    0.14 | 5.2185 | 0.6409 |  131441 B |      178.59 |
| Decrypt_Short   | MediumRun  | 15             | 2           | 10          |  3.248 μs | 0.0033 μs | 0.0046 μs |  3.247 μs |  0.68 |    0.00 | 0.0153 |      - |     448 B |        0.61 |
| Encrypt_Short   | MediumRun  | 15             | 2           | 10          |  4.782 μs | 0.0133 μs | 0.0198 μs |  4.787 μs |  1.00 |    0.01 | 0.0229 |      - |     736 B |        1.00 |
| Roundtrip_Short | MediumRun  | 15             | 2           | 10          |  8.150 μs | 0.0506 μs | 0.0726 μs |  8.107 μs |  1.70 |    0.02 | 0.0458 |      - |    1184 B |        1.61 |
