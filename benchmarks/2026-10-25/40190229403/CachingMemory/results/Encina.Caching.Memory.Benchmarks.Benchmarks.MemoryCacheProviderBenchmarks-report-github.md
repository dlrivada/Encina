```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error    | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|---------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  44.97 ns | 4.362 ns | 0.239 ns |  0.33 |      - |         - |        0.00 |
| ExistsAsync_True   |  50.17 ns | 1.144 ns | 0.063 ns |  0.37 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 134.33 ns | 1.919 ns | 0.105 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss |  97.61 ns | 0.763 ns | 0.042 ns |  0.73 |      - |         - |        0.00 |
