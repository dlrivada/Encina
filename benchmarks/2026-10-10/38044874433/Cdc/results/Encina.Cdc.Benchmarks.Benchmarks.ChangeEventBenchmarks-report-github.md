```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean       | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |-----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 17.5757 ns | 2.2144 ns | 0.1214 ns |  0.63 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  0.9796 ns | 0.0289 ns | 0.0016 ns |  0.04 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 10.5115 ns | 1.2939 ns | 0.0709 ns |  0.38 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 27.9683 ns | 1.1027 ns | 0.0604 ns |  1.00 | 0.0033 |      56 B |        1.00 |
