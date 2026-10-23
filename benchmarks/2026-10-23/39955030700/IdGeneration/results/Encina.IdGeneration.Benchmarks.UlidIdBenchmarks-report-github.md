```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method            | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|--------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 1,014.1 ns |   4.72 ns |  2.81 ns |  1.21 |    0.00 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     |   824.6 ns |   4.88 ns |  2.90 ns |  0.99 |    0.00 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     |   834.8 ns |   2.18 ns |  1.44 ns |  1.00 |    0.00 |      40 B |        1.00 |
|                   |            |                |             |            |           |          |       |         |           |             |
| Generate_ToString | ShortRun   | 3              | 1           | 1,033.8 ns | 567.28 ns | 31.09 ns |  1.24 |    0.03 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           |   826.4 ns |  18.71 ns |  1.03 ns |  0.99 |    0.00 |      40 B |        1.00 |
| Generate          | ShortRun   | 3              | 1           |   834.8 ns |  49.06 ns |  2.69 ns |  1.00 |    0.00 |      40 B |        1.00 |
