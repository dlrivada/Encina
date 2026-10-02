```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 25.618 ns | 7.1850 ns | 0.3938 ns |  0.66 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.264 ns | 0.0994 ns | 0.0054 ns |  0.03 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 14.375 ns | 3.8192 ns | 0.2093 ns |  0.37 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 39.038 ns | 0.5310 ns | 0.0291 ns |  1.00 | 0.0033 |      56 B |        1.00 |
