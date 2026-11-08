```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.07GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method             | Mean      | Error    | StdDev   | Median    | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|---------:|---------:|----------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  52.92 ns | 0.105 ns | 0.147 ns |  52.99 ns |  0.29 |      - |         - |        0.00 |
| ExistsAsync_True   |  61.26 ns | 0.767 ns | 1.148 ns |  61.28 ns |  0.33 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 184.75 ns | 0.752 ns | 1.126 ns | 184.83 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss | 128.18 ns | 0.492 ns | 0.657 ns | 128.63 ns |  0.69 |      - |         - |        0.00 |
