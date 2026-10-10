```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method            | Job        | IterationCount | LaunchCount | Mean       | Error       | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |-----------:|------------:|---------:|------:|--------:|-------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 1,059.9 ns |    11.98 ns |  7.92 ns |  1.08 |    0.01 | 0.0057 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     |   961.8 ns |     3.82 ns |  2.28 ns |  0.98 |    0.00 | 0.0019 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     |   980.7 ns |     5.79 ns |  3.45 ns |  1.00 |    0.00 | 0.0019 |      40 B |        1.00 |
|                   |            |                |             |            |             |          |       |         |        |           |             |
| Generate_ToString | ShortRun   | 3              | 1           | 1,049.7 ns |    80.80 ns |  4.43 ns |  1.03 |    0.05 | 0.0057 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           |   892.9 ns |    29.76 ns |  1.63 ns |  0.87 |    0.04 | 0.0019 |      40 B |        1.00 |
| Generate          | ShortRun   | 3              | 1           | 1,022.7 ns | 1,001.83 ns | 54.91 ns |  1.00 |    0.06 | 0.0019 |      40 B |        1.00 |
