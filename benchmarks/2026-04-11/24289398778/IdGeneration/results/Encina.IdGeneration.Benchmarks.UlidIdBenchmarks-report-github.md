```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method            | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |------------ |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 3           | 1.236 μs | 0.0036 μs | 0.0021 μs |  1.10 | 0.0057 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     | 3           | 1.061 μs | 0.0073 μs | 0.0048 μs |  0.95 | 0.0019 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     | 3           | 1.120 μs | 0.0025 μs | 0.0015 μs |  1.00 | 0.0019 |      40 B |        1.00 |
|                   |            |                |             |             |          |           |           |       |        |           |             |
| Generate_ToString | MediumRun  | 15             | 2           | 10          | 1.175 μs | 0.0047 μs | 0.0067 μs |  1.08 | 0.0057 |     120 B |        3.00 |
| NewUlid_Direct    | MediumRun  | 15             | 2           | 10          | 1.048 μs | 0.0037 μs | 0.0055 μs |  0.96 | 0.0019 |      40 B |        1.00 |
| Generate          | MediumRun  | 15             | 2           | 10          | 1.092 μs | 0.0049 μs | 0.0070 μs |  1.00 | 0.0019 |      40 B |        1.00 |
