```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method          | Job        | IterationCount | LaunchCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------- |----------- |--------------- |------------ |----------:|-----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     |  5.363 μs |  0.0132 μs | 0.0087 μs |  1.11 |    0.00 | 0.1450 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | 27.124 μs |  1.2057 μs | 0.7975 μs |  5.62 |    0.16 | 7.8125 | 1.7090 |  197226 B |      267.97 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     |  3.625 μs |  0.0082 μs | 0.0043 μs |  0.75 |    0.00 | 0.0954 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | 18.998 μs |  0.4695 μs | 0.3105 μs |  3.94 |    0.06 | 5.2185 | 0.6409 |  131441 B |      178.59 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     |  3.221 μs |  0.0062 μs | 0.0037 μs |  0.67 |    0.00 | 0.0153 |      - |     448 B |        0.61 |
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     |  4.822 μs |  0.0246 μs | 0.0163 μs |  1.00 |    0.00 | 0.0229 |      - |     736 B |        1.00 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     |  8.433 μs |  0.0320 μs | 0.0212 μs |  1.75 |    0.01 | 0.0458 |      - |    1184 B |        1.61 |
|                 |            |                |             |           |            |           |       |         |        |        |           |             |
| Encrypt_Medium  | ShortRun   | 3              | 1           |  5.374 μs |  0.0812 μs | 0.0045 μs |  1.12 |    0.00 | 0.1450 |      - |    3688 B |        5.01 |
| Encrypt_Large   | ShortRun   | 3              | 1           | 28.851 μs | 18.8757 μs | 1.0346 μs |  6.04 |    0.19 | 7.8125 | 1.7090 |  197226 B |      267.97 |
| Decrypt_Medium  | ShortRun   | 3              | 1           |  3.697 μs |  0.2869 μs | 0.0157 μs |  0.77 |    0.00 | 0.0954 |      - |    2416 B |        3.28 |
| Decrypt_Large   | ShortRun   | 3              | 1           | 20.669 μs |  1.4266 μs | 0.0782 μs |  4.33 |    0.02 | 5.2185 | 0.6409 |  131441 B |      178.59 |
| Decrypt_Short   | ShortRun   | 3              | 1           |  3.278 μs |  0.2317 μs | 0.0127 μs |  0.69 |    0.00 | 0.0153 |      - |     448 B |        0.61 |
| Encrypt_Short   | ShortRun   | 3              | 1           |  4.779 μs |  0.2639 μs | 0.0145 μs |  1.00 |    0.00 | 0.0229 |      - |     736 B |        1.00 |
| Roundtrip_Short | ShortRun   | 3              | 1           |  8.469 μs |  1.4374 μs | 0.0788 μs |  1.77 |    0.02 | 0.0458 |      - |    1184 B |        1.61 |
