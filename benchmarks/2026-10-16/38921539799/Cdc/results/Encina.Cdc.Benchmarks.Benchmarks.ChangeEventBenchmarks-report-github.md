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
| CreateChangeEvent          | 19.5518 ns | 0.4280 ns | 0.0235 ns |  0.69 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  0.9819 ns | 0.0492 ns | 0.0027 ns |  0.03 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 10.9957 ns | 3.7011 ns | 0.2029 ns |  0.39 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 28.4235 ns | 0.9248 ns | 0.0507 ns |  1.00 | 0.0033 |      56 B |        1.00 |
