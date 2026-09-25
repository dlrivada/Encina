```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method               | Job        | IterationCount | LaunchCount | Mean     | Error   | StdDev  | Ratio | Allocated | Alloc Ratio |
|--------------------- |----------- |--------------- |------------ |---------:|--------:|--------:|------:|----------:|------------:|
| Generate_WithShardId | Job-YFEFPZ | 10             | Default     | 241.4 ns | 0.10 ns | 0.06 ns |  1.00 |         - |          NA |
| GenerateAndGetValue  | Job-YFEFPZ | 10             | Default     | 241.3 ns | 0.13 ns | 0.09 ns |  1.00 |         - |          NA |
| Generate             | Job-YFEFPZ | 10             | Default     | 241.4 ns | 0.15 ns | 0.10 ns |  1.00 |         - |          NA |
| ExtractShardId       | Job-YFEFPZ | 10             | Default     | 239.3 ns | 0.05 ns | 0.03 ns |  0.99 |         - |          NA |
|                      |            |                |             |          |         |         |       |           |             |
| Generate_WithShardId | ShortRun   | 3              | 1           | 241.4 ns | 0.51 ns | 0.03 ns |  1.00 |         - |          NA |
| GenerateAndGetValue  | ShortRun   | 3              | 1           | 241.3 ns | 2.40 ns | 0.13 ns |  1.00 |         - |          NA |
| Generate             | ShortRun   | 3              | 1           | 241.4 ns | 0.55 ns | 0.03 ns |  1.00 |         - |          NA |
| ExtractShardId       | ShortRun   | 3              | 1           | 239.1 ns | 1.73 ns | 0.09 ns |  0.99 |         - |          NA |
