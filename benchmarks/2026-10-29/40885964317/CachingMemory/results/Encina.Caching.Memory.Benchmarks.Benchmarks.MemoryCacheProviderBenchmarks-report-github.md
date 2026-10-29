```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 3.94GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  38.11 ns |  3.583 ns | 0.196 ns |  0.33 |      - |         - |        0.00 |
| ExistsAsync_True   |  43.24 ns |  2.652 ns | 0.145 ns |  0.38 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 114.40 ns | 16.538 ns | 0.907 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss |  85.71 ns | 20.602 ns | 1.129 ns |  0.75 |      - |         - |        0.00 |
