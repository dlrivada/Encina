```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method            | Job        | IterationCount | LaunchCount | Mean       | Error    | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |-----------:|---------:|--------:|------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 1,214.3 ns | 10.53 ns | 6.96 ns |  1.22 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     |   975.4 ns |  7.74 ns | 4.60 ns |  0.98 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     |   997.8 ns |  9.48 ns | 6.27 ns |  1.00 |      40 B |        1.00 |
|                   |            |                |             |            |          |         |       |           |             |
| Generate_ToString | ShortRun   | 3              | 1           | 1,203.7 ns | 73.14 ns | 4.01 ns |  1.21 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           |   984.3 ns | 96.89 ns | 5.31 ns |  0.99 |      40 B |        1.00 |
| Generate          | ShortRun   | 3              | 1           |   998.8 ns | 34.55 ns | 1.89 ns |  1.00 |      40 B |        1.00 |
