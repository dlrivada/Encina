```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method            | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|-------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 1,003.4 ns |   6.20 ns |  4.10 ns |  1.09 | 0.0057 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     |   963.7 ns |   1.51 ns |  0.90 ns |  1.05 | 0.0019 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     |   917.3 ns |   3.42 ns |  2.26 ns |  1.00 | 0.0019 |      40 B |        1.00 |
|                   |            |                |             |            |           |          |       |        |           |             |
| Generate_ToString | ShortRun   | 3              | 1           |   996.8 ns | 150.35 ns |  8.24 ns |  1.08 | 0.0057 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           |   966.9 ns | 186.87 ns | 10.24 ns |  1.04 | 0.0019 |      40 B |        1.00 |
| Generate          | ShortRun   | 3              | 1           |   926.9 ns |  63.61 ns |  3.49 ns |  1.00 | 0.0019 |      40 B |        1.00 |
