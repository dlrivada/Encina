```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.96GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 963.1 ns |   7.07 ns |  4.68 ns |  1.00 |    0.01 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 425.5 ns |   3.37 ns |  2.23 ns |  0.44 |    0.00 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 962.6 ns |   5.86 ns |  3.49 ns |  1.00 |    0.00 |         - |          NA |
|                    |            |                |             |          |           |          |       |         |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 963.8 ns | 349.12 ns | 19.14 ns |  0.99 |    0.02 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 425.9 ns |  74.96 ns |  4.11 ns |  0.44 |    0.01 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 974.8 ns | 214.80 ns | 11.77 ns |  1.00 |    0.01 |         - |          NA |
