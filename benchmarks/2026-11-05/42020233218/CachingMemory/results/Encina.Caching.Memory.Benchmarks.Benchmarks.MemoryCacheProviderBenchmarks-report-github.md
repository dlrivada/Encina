```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.65GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  44.41 ns | 11.230 ns | 0.616 ns |  0.31 |      - |         - |        0.00 |
| ExistsAsync_True   |  50.02 ns |  1.334 ns | 0.073 ns |  0.35 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 141.40 ns |  2.009 ns | 0.110 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss |  97.44 ns |  0.599 ns | 0.033 ns |  0.69 |      - |         - |        0.00 |
