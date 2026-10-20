```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method            | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|-------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 1,027.0 ns |   2.44 ns |  1.61 ns |  1.06 | 0.0038 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     |   928.3 ns |   1.67 ns |  1.00 ns |  0.96 | 0.0010 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     |   970.1 ns |   2.33 ns |  1.22 ns |  1.00 |      - |      40 B |        1.00 |
|                   |            |                |             |            |           |          |       |        |           |             |
| Generate_ToString | ShortRun   | 3              | 1           | 1,034.0 ns | 200.98 ns | 11.02 ns |  1.05 | 0.0038 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           |   924.4 ns |   6.82 ns |  0.37 ns |  0.94 | 0.0010 |      40 B |        1.00 |
| Generate          | ShortRun   | 3              | 1           |   980.6 ns |  37.80 ns |  2.07 ns |  1.00 |      - |      40 B |        1.00 |
