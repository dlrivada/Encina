```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  43.77 ns |  1.372 ns | 0.075 ns |  0.32 |      - |         - |        0.00 |
| ExistsAsync_True   |  50.44 ns |  0.380 ns | 0.021 ns |  0.37 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 137.65 ns |  9.221 ns | 0.505 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss |  98.79 ns | 33.177 ns | 1.819 ns |  0.72 |      - |         - |        0.00 |
