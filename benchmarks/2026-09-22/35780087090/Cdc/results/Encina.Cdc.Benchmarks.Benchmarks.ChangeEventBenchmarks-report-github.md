```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.71GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 22.715 ns | 4.9025 ns | 0.2687 ns |  0.63 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.385 ns | 0.0342 ns | 0.0019 ns |  0.04 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 13.027 ns | 7.5467 ns | 0.4137 ns |  0.36 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 35.969 ns | 1.9708 ns | 0.1080 ns |  1.00 | 0.0033 |      56 B |        1.00 |
