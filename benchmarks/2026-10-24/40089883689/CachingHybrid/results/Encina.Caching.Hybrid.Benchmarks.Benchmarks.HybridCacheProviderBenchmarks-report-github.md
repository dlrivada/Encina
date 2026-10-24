```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean       | Error    | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |-----------:|---------:|--------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |   772.8 ns | 69.96 ns | 3.83 ns |  0.77 | 0.0362 |     616 B |        1.24 |
| ExistsAsync_True   |   878.8 ns | 91.59 ns | 5.02 ns |  0.87 | 0.0362 |     616 B |        1.24 |
| GetAsync_CacheHit  | 1,007.0 ns | 81.09 ns | 4.44 ns |  1.00 | 0.0286 |     496 B |        1.00 |
| GetAsync_CacheMiss |   783.0 ns | 59.06 ns | 3.24 ns |  0.78 | 0.0362 |     616 B |        1.24 |
