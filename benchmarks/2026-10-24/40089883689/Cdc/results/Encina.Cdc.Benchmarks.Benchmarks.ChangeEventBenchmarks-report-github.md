```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 18.225 ns | 7.0166 ns | 0.3846 ns |  0.65 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.023 ns | 0.0186 ns | 0.0010 ns |  0.04 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 11.116 ns | 1.9761 ns | 0.1083 ns |  0.40 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 27.858 ns | 1.1251 ns | 0.0617 ns |  1.00 | 0.0033 |      56 B |        1.00 |
