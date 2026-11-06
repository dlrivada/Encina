```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method               | Job        | IterationCount | LaunchCount | Mean     | Error   | StdDev  | Ratio | Allocated | Alloc Ratio |
|--------------------- |----------- |--------------- |------------ |---------:|--------:|--------:|------:|----------:|------------:|
| Generate_WithShardId | Job-YFEFPZ | 10             | Default     | 241.2 ns | 0.12 ns | 0.08 ns |  1.00 |         - |          NA |
| GenerateAndGetValue  | Job-YFEFPZ | 10             | Default     | 241.2 ns | 0.13 ns | 0.07 ns |  1.00 |         - |          NA |
| Generate             | Job-YFEFPZ | 10             | Default     | 241.3 ns | 0.06 ns | 0.04 ns |  1.00 |         - |          NA |
| ExtractShardId       | Job-YFEFPZ | 10             | Default     | 239.1 ns | 0.05 ns | 0.03 ns |  0.99 |         - |          NA |
|                      |            |                |             |          |         |         |       |           |             |
| Generate_WithShardId | ShortRun   | 3              | 1           | 241.2 ns | 3.21 ns | 0.18 ns |  1.00 |         - |          NA |
| GenerateAndGetValue  | ShortRun   | 3              | 1           | 241.2 ns | 3.02 ns | 0.17 ns |  1.00 |         - |          NA |
| Generate             | ShortRun   | 3              | 1           | 241.2 ns | 2.51 ns | 0.14 ns |  1.00 |         - |          NA |
| ExtractShardId       | ShortRun   | 3              | 1           | 239.1 ns | 0.91 ns | 0.05 ns |  0.99 |         - |          NA |
