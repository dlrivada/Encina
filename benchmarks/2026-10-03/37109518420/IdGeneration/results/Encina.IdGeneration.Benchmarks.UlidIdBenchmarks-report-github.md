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
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 1,010.1 ns |  7.00 ns | 4.17 ns |  1.22 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     |   818.5 ns |  4.49 ns | 2.67 ns |  0.99 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     |   830.1 ns |  5.57 ns | 3.31 ns |  1.00 |      40 B |        1.00 |
|                   |            |                |             |            |          |         |       |           |             |
| Generate_ToString | ShortRun   | 3              | 1           | 1,013.1 ns | 81.75 ns | 4.48 ns |  1.23 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           |   813.9 ns | 56.02 ns | 3.07 ns |  0.99 |      40 B |        1.00 |
| Generate          | ShortRun   | 3              | 1           |   826.0 ns | 34.07 ns | 1.87 ns |  1.00 |      40 B |        1.00 |
