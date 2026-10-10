```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.77GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.987 ns |  0.1596 ns | 0.0087 ns |   0.47 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.450 ns |  2.2184 ns | 0.1216 ns |   1.00 |    0.02 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   785.699 ns | 94.5183 ns | 5.1809 ns |  93.00 |    1.28 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,186.089 ns | 37.5969 ns | 2.0608 ns | 140.39 |    1.77 | 0.0286 |     504 B |       12.60 |
