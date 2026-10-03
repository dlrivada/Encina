```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.52GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean       | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |-----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 11.8866 ns | 1.8373 ns | 0.1007 ns |  0.50 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  0.4570 ns | 0.3005 ns | 0.0165 ns |  0.02 |      - |         - |        0.00 |
| ChangeEvent_WithExpression |  7.3960 ns | 4.1665 ns | 0.2284 ns |  0.31 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 23.5401 ns | 0.6809 ns | 0.0373 ns |  1.00 | 0.0033 |      56 B |        1.00 |
