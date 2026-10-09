```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean       | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |-----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 13.5276 ns | 7.1302 ns | 0.3908 ns |  0.51 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  0.5346 ns | 0.4812 ns | 0.0264 ns |  0.02 |      - |         - |        0.00 |
| ChangeEvent_WithExpression |  8.2416 ns | 2.3189 ns | 0.1271 ns |  0.31 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 26.7609 ns | 2.2119 ns | 0.1212 ns |  1.00 | 0.0033 |      56 B |        1.00 |
