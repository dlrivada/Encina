```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method            | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 1,229.4 ns |  16.24 ns |  9.66 ns |  1.23 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     | 1,000.3 ns |   7.62 ns |  5.04 ns |  1.00 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     | 1,003.6 ns |   5.33 ns |  3.53 ns |  1.00 |      40 B |        1.00 |
|                   |            |                |             |            |           |          |       |           |             |
| Generate_ToString | ShortRun   | 3              | 1           | 1,194.7 ns | 235.79 ns | 12.92 ns |  1.18 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           |   994.5 ns |  30.52 ns |  1.67 ns |  0.99 |      40 B |        1.00 |
| Generate          | ShortRun   | 3              | 1           | 1,008.6 ns | 175.16 ns |  9.60 ns |  1.00 |      40 B |        1.00 |
