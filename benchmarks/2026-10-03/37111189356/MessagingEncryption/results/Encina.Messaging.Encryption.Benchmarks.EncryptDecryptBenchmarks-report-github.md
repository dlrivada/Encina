```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method          | Job        | IterationCount | LaunchCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------- |----------- |--------------- |------------ |----------:|-----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     |  5.199 μs |  0.0212 μs | 0.0140 μs |  1.11 |    0.01 | 0.0381 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | 24.333 μs |  1.3759 μs | 0.9101 μs |  5.21 |    0.19 | 2.3499 | 0.5188 |  197225 B |      267.97 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     |  3.644 μs |  0.0732 μs | 0.0484 μs |  0.78 |    0.01 | 0.0267 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | 18.077 μs |  1.0461 μs | 0.6919 μs |  3.87 |    0.15 | 1.5564 |      - |  131440 B |      178.59 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     |  3.258 μs |  0.0640 μs | 0.0423 μs |  0.70 |    0.01 | 0.0038 |      - |     448 B |        0.61 |
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     |  4.668 μs |  0.0745 μs | 0.0443 μs |  1.00 |    0.01 | 0.0076 |      - |     736 B |        1.00 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     |  8.107 μs |  0.1086 μs | 0.0718 μs |  1.74 |    0.02 |      - |      - |    1184 B |        1.61 |
|                 |            |                |             |           |            |           |       |         |        |        |           |             |
| Encrypt_Medium  | ShortRun   | 3              | 1           |  5.252 μs |  2.4023 μs | 0.1317 μs |  1.11 |    0.03 | 0.0381 |      - |    3688 B |        5.01 |
| Encrypt_Large   | ShortRun   | 3              | 1           | 25.660 μs | 15.8011 μs | 0.8661 μs |  5.41 |    0.18 | 2.3499 | 0.5188 |  197225 B |      267.97 |
| Decrypt_Medium  | ShortRun   | 3              | 1           |  3.555 μs |  0.3145 μs | 0.0172 μs |  0.75 |    0.01 | 0.0267 |      - |    2416 B |        3.28 |
| Decrypt_Large   | ShortRun   | 3              | 1           | 18.834 μs | 16.9346 μs | 0.9282 μs |  3.97 |    0.18 | 1.5564 |      - |  131440 B |      178.59 |
| Decrypt_Short   | ShortRun   | 3              | 1           |  3.228 μs |  1.0179 μs | 0.0558 μs |  0.68 |    0.01 | 0.0038 |      - |     448 B |        0.61 |
| Encrypt_Short   | ShortRun   | 3              | 1           |  4.743 μs |  1.4697 μs | 0.0806 μs |  1.00 |    0.02 | 0.0076 |      - |     736 B |        1.00 |
| Roundtrip_Short | ShortRun   | 3              | 1           |  7.971 μs |  0.7857 μs | 0.0431 μs |  1.68 |    0.03 |      - |      - |    1184 B |        1.61 |
