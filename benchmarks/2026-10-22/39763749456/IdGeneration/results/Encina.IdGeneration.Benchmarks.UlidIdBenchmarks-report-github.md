```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.67GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method            | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |-----------:|----------:|--------:|------:|-------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     |   992.2 ns |   2.77 ns | 1.65 ns |  1.09 | 0.0057 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     |   887.3 ns |   4.59 ns | 2.40 ns |  0.97 | 0.0019 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     |   914.5 ns |   3.58 ns | 2.13 ns |  1.00 | 0.0019 |      40 B |        1.00 |
|                   |            |                |             |            |           |         |       |        |           |             |
| Generate_ToString | ShortRun   | 3              | 1           | 1,053.2 ns |  46.76 ns | 2.56 ns |  1.08 | 0.0057 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           |   891.7 ns |  37.71 ns | 2.07 ns |  0.92 | 0.0019 |      40 B |        1.00 |
| Generate          | ShortRun   | 3              | 1           |   971.9 ns | 135.52 ns | 7.43 ns |  1.00 | 0.0019 |      40 B |        1.00 |
