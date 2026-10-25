```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean       | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |-----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 12.7231 ns | 1.9064 ns | 0.1045 ns |  0.53 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  0.4593 ns | 0.4370 ns | 0.0240 ns |  0.02 |      - |         - |        0.00 |
| ChangeEvent_WithExpression |  7.7659 ns | 1.6577 ns | 0.0909 ns |  0.32 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 24.1924 ns | 5.1894 ns | 0.2844 ns |  1.00 | 0.0033 |      56 B |        1.00 |
