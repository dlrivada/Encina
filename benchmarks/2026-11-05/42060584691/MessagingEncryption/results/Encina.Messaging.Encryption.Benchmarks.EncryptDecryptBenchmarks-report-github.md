```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method          | Job        | IterationCount | LaunchCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------- |----------- |--------------- |------------ |----------:|-----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     |  4.440 μs |  0.0244 μs | 0.0145 μs |  1.14 |    0.01 | 0.1450 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | 29.523 μs |  1.3817 μs | 0.9139 μs |  7.59 |    0.24 | 7.8125 | 1.7090 |  197226 B |      267.97 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     |  3.136 μs |  0.0112 μs | 0.0067 μs |  0.81 |    0.01 | 0.0954 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | 22.065 μs |  0.3051 μs | 0.2018 μs |  5.68 |    0.08 | 5.2185 | 0.6409 |  131441 B |      178.59 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     |  2.725 μs |  0.0064 μs | 0.0042 μs |  0.70 |    0.01 | 0.0153 |      - |     448 B |        0.61 |
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     |  3.888 μs |  0.0688 μs | 0.0455 μs |  1.00 |    0.02 | 0.0229 |      - |     736 B |        1.00 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     |  6.699 μs |  0.0113 μs | 0.0067 μs |  1.72 |    0.02 | 0.0458 |      - |    1184 B |        1.61 |
|                 |            |                |             |           |            |           |       |         |        |        |           |             |
| Encrypt_Medium  | ShortRun   | 3              | 1           |  4.555 μs |  0.0912 μs | 0.0050 μs |  1.16 |    0.01 | 0.1450 |      - |    3688 B |        5.01 |
| Encrypt_Large   | ShortRun   | 3              | 1           | 33.644 μs | 18.2302 μs | 0.9993 μs |  8.53 |    0.22 | 7.8125 | 1.7090 |  197226 B |      267.97 |
| Decrypt_Medium  | ShortRun   | 3              | 1           |  3.200 μs |  0.0673 μs | 0.0037 μs |  0.81 |    0.00 | 0.0954 |      - |    2416 B |        3.28 |
| Decrypt_Large   | ShortRun   | 3              | 1           | 23.491 μs |  5.3203 μs | 0.2916 μs |  5.96 |    0.07 | 5.2185 | 0.6409 |  131441 B |      178.59 |
| Decrypt_Short   | ShortRun   | 3              | 1           |  2.826 μs |  0.3031 μs | 0.0166 μs |  0.72 |    0.00 | 0.0153 |      - |     448 B |        0.61 |
| Encrypt_Short   | ShortRun   | 3              | 1           |  3.944 μs |  0.3618 μs | 0.0198 μs |  1.00 |    0.01 | 0.0229 |      - |     736 B |        1.00 |
| Roundtrip_Short | ShortRun   | 3              | 1           |  6.867 μs |  1.6969 μs | 0.0930 μs |  1.74 |    0.02 | 0.0458 |      - |    1184 B |        1.61 |
