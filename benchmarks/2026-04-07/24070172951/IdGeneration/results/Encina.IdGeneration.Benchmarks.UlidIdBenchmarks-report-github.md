```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.65GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method            | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| Generate          | Job-YFEFPZ | 10             | Default     | 1.096 μs | 0.0058 μs | 0.0039 μs |  1.00 | 0.0019 |      40 B |        1.00 |
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 1.251 μs | 0.0050 μs | 0.0030 μs |  1.14 | 0.0057 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     | 1.059 μs | 0.0032 μs | 0.0019 μs |  0.97 | 0.0019 |      40 B |        1.00 |
|                   |            |                |             |          |           |           |       |        |           |             |
| Generate          | ShortRun   | 3              | 1           | 1.160 μs | 0.0897 μs | 0.0049 μs |  1.00 | 0.0019 |      40 B |        1.00 |
| Generate_ToString | ShortRun   | 3              | 1           | 1.181 μs | 0.1096 μs | 0.0060 μs |  1.02 | 0.0057 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           | 1.045 μs | 0.0652 μs | 0.0036 μs |  0.90 | 0.0019 |      40 B |        1.00 |
