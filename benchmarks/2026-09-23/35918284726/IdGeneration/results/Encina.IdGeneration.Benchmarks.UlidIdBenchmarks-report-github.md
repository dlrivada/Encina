```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method            | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 1.216 μs | 0.0033 μs | 0.0022 μs |  1.03 | 0.0057 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     | 1.067 μs | 0.0061 μs | 0.0040 μs |  0.91 | 0.0019 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     | 1.176 μs | 0.0070 μs | 0.0042 μs |  1.00 | 0.0019 |      40 B |        1.00 |
|                   |            |                |             |          |           |           |       |        |           |             |
| Generate_ToString | ShortRun   | 3              | 1           | 1.173 μs | 0.0652 μs | 0.0036 μs |  1.07 | 0.0057 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           | 1.065 μs | 0.1165 μs | 0.0064 μs |  0.97 | 0.0019 |      40 B |        1.00 |
| Generate          | ShortRun   | 3              | 1           | 1.099 μs | 0.1638 μs | 0.0090 μs |  1.00 | 0.0019 |      40 B |        1.00 |
