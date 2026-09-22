```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error     | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.500 ns |  2.142 ns | 0.1174 ns |   0.37 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     9.370 ns |  2.316 ns | 0.1270 ns |   1.00 |    0.02 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   719.382 ns | 21.532 ns | 1.1803 ns |  76.78 |    0.91 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,066.623 ns | 15.140 ns | 0.8299 ns | 113.85 |    1.35 | 0.0286 |     504 B |       12.60 |
