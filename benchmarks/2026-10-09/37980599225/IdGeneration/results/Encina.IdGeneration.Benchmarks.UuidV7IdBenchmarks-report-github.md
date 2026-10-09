```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 968.9 ns |  17.57 ns | 11.62 ns |  1.02 |    0.02 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 422.9 ns |  13.80 ns |  9.12 ns |  0.44 |    0.01 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 952.3 ns |  16.90 ns | 11.18 ns |  1.00 |    0.02 |         - |          NA |
|                    |            |                |             |          |           |          |       |         |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 946.6 ns | 228.63 ns | 12.53 ns |  1.08 |    0.01 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 432.8 ns | 126.77 ns |  6.95 ns |  0.49 |    0.01 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 878.1 ns |  98.82 ns |  5.42 ns |  1.00 |    0.01 |         - |          NA |
