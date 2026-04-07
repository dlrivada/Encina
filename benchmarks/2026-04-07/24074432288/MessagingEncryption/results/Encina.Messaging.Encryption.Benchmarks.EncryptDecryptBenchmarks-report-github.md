```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4


```
| Method          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     | 3           |  4.842 μs | 0.0176 μs | 0.0117 μs |  1.00 |    0.00 | 0.0229 |      - |     736 B |        1.00 |
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     | 3           |  5.377 μs | 0.0277 μs | 0.0183 μs |  1.11 |    0.00 | 0.1450 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | 3           | 22.922 μs | 0.5017 μs | 0.2986 μs |  4.73 |    0.06 | 7.8125 | 1.7090 |  197226 B |      267.97 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     | 3           |  3.326 μs | 0.0101 μs | 0.0067 μs |  0.69 |    0.00 | 0.0153 |      - |     448 B |        0.61 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     | 3           |  3.724 μs | 0.0142 μs | 0.0074 μs |  0.77 |    0.00 | 0.0954 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | 3           | 17.982 μs | 0.8239 μs | 0.5450 μs |  3.71 |    0.11 | 5.2185 | 0.6409 |  131441 B |      178.59 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     | 3           |  8.260 μs | 0.0293 μs | 0.0194 μs |  1.71 |    0.01 | 0.0458 |      - |    1184 B |        1.61 |
|                 |            |                |             |             |           |           |           |       |         |        |        |           |             |
| Encrypt_Short   | MediumRun  | 15             | 2           | 10          |  4.784 μs | 0.0107 μs | 0.0149 μs |  1.00 |    0.00 | 0.0229 |      - |     736 B |        1.00 |
| Encrypt_Medium  | MediumRun  | 15             | 2           | 10          |  5.324 μs | 0.0203 μs | 0.0291 μs |  1.11 |    0.01 | 0.1450 |      - |    3688 B |        5.01 |
| Encrypt_Large   | MediumRun  | 15             | 2           | 10          | 22.830 μs | 0.2306 μs | 0.3308 μs |  4.77 |    0.07 | 7.8125 | 1.7090 |  197226 B |      267.97 |
| Decrypt_Short   | MediumRun  | 15             | 2           | 10          |  3.315 μs | 0.0134 μs | 0.0188 μs |  0.69 |    0.00 | 0.0153 |      - |     448 B |        0.61 |
| Decrypt_Medium  | MediumRun  | 15             | 2           | 10          |  3.710 μs | 0.0079 μs | 0.0116 μs |  0.78 |    0.00 | 0.0954 |      - |    2416 B |        3.28 |
| Decrypt_Large   | MediumRun  | 15             | 2           | 10          | 16.958 μs | 0.2945 μs | 0.4407 μs |  3.54 |    0.09 | 5.2185 | 0.6409 |  131441 B |      178.59 |
| Roundtrip_Short | MediumRun  | 15             | 2           | 10          |  8.405 μs | 0.0332 μs | 0.0466 μs |  1.76 |    0.01 | 0.0458 |      - |    1184 B |        1.61 |
