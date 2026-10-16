```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean       | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |-----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 18.0822 ns | 1.7960 ns | 0.0984 ns |  0.53 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  0.9843 ns | 0.0248 ns | 0.0014 ns |  0.03 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 10.3217 ns | 2.7061 ns | 0.1483 ns |  0.30 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 33.8539 ns | 4.6390 ns | 0.2543 ns |  1.00 | 0.0033 |      56 B |        1.00 |
