```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error         | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|--------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     37.86 ns |      2.954 ns |   0.162 ns |     1.28 |    0.04 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     29.59 ns |     16.896 ns |   0.926 ns |     1.00 |    0.04 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 91,575.18 ns | 12,635.215 ns | 692.579 ns | 3,097.11 |   86.79 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    142.35 ns |      7.832 ns |   0.429 ns |     4.81 |    0.13 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     76.50 ns |     23.285 ns |   1.276 ns |     2.59 |    0.08 | 0.0105 |      - |     176 B |        1.57 |
